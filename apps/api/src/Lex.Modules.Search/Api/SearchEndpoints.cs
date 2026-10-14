using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql; 
using Lex.Modules.Search.Domain.ValueObjects;
using Lex.Modules.Search.Application.Services; 

namespace Lex.Modules.Search.Api;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search");

        // 1. MEVCUT HİBRİT ARAMA ENDPOINT'İ (Cam gibi netleştirildi, uyarılar silindi)
        group.MapGet("/", async (
            [FromQuery] string q,
            [FromQuery] string[]? chamberCodes, 
            [FromQuery] DateOnly? startDate,    
            [FromQuery] DateOnly? endDate,      
            [FromServices] HybridSearchService hybridSearchService, 
            [FromServices] IConfiguration config,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { Error = "Dayı boş arama yapılamaz, bir şeyler yaz!" });

            var filters = new SearchFilterSet(
                chamberCodes: chamberCodes?.ToList(),
                startDate: startDate,
                endDate: endDate
            );

            var pagination = new Pagination(1, 10); 
            var hybridResults = await hybridSearchService.SearchAsync(q, filters, pagination, ct);

            var allIds = hybridResults.Select(h => h.DecisionId).Distinct().ToList();
            var metadataDict = new Dictionary<Guid, dynamic>();

            if (allIds.Any())
            {
                var connStr = config.GetConnectionString("DefaultConnection");
                await using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync(ct);

                var query = @"
                    SELECT ""Id"", ""ChamberName"", ""CaseNumber"", ""DecisionNumber"", ""DecisionDate"" 
                    FROM decisions.court_decisions 
                    WHERE ""Id"" = ANY(@ids)";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("ids", allIds.ToArray());
                
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var id = reader.GetGuid(0);
                    metadataDict[id] = new {
                        Chamber = reader.IsDBNull(1) ? "Bilinmeyen Daire" : reader.GetString(1),
                        Case = reader.IsDBNull(2) ? "Bilinmiyor" : reader.GetString(2),
                        Decision = reader.IsDBNull(3) ? "Bilinmiyor" : reader.GetString(3),
                        Date = reader.IsDBNull(4) ? "Bilinmiyor" : reader.GetValue(4).ToString() ?? "Bilinmiyor"
                    };
                }
            }

            var finalFusedList = hybridResults.Select(h => {
                metadataDict.TryGetValue(h.DecisionId, out var meta);
                
                string resolvedChamber = meta?.Chamber ?? h.ChamberCode ?? "Bilinmeyen Daire";
                string resolvedCase = meta?.Case ?? h.CaseNumber ?? "Bilinmiyor";
                string resolvedDecision = meta?.Decision ?? h.DecisionNumber ?? "Bilinmiyor";
                string resolvedDate = meta?.Date ?? h.DecisionDate.ToString();

                string resolvedSnippet = h.Snippet;
                if (string.IsNullOrWhiteSpace(resolvedSnippet)) 
                {
                    resolvedSnippet = "💡 Bu karar, aradığınız kavramlarla anlamsal (yapay zeka) olarak eşleştiği için doğrudan kelime vurgusu içermemektedir.";
                }

                return new {
                    DecisionId = h.DecisionId,
                    ChamberCode = resolvedChamber.Replace("_", " "), 
                    CaseNumber = resolvedCase,
                    DecisionNumber = resolvedDecision,
                    DecisionDate = resolvedDate,
                    Snippet = resolvedSnippet,
                    RrfScore = Math.Round(h.RrfScore, 4) 
                };
            }).ToList();

            return Results.Ok(new
            {
                Query = q,
                TotalCount = finalFusedList.Count, 
                Results = finalFusedList
            });
        });

        // 2. YENİ EKLENEN YAPAY ZEKA ASİSTAN ENDPOINT'İ (STREAMING) 🚀
        // Tarayıcı veya Frontend buraya bağlandığında cevap kelime kelime ekrana akacak!
        group.MapGet("/chat", async (
            [FromQuery] string q,
            [FromServices] LegalAssistantService assistantService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsJsonAsync(new { Error = "Soru boş olamaz dayı!" }, cancellationToken: ct);
                return;
            }

            // HTTP Response başlıklarını "Streaming" (text/event-stream veya plain text) formatına ayarlıyoruz
            httpContext.Response.ContentType = "text/plain; charset=utf-8";
            
            try
            {
                // Canavar servisimizden akışı alıyoruz
                var tokenStream = assistantService.AnswerLegalQuestionStreamAsync(q, ct);

                await foreach (var token in tokenStream)
                {
                    // Her bir kelime (token) geldiği an HTTP boru hattından tarayıcıya fırlatılıyor!
                    await httpContext.Response.WriteAsync(token, ct);
                    await httpContext.Response.Body.FlushAsync(ct); // Tamponu boşalt ki anında ekrana düşsün
                }
            }
            catch (Exception ex)
            {
                // Akış esnasında bir sorun olursa hata logu bas
                await httpContext.Response.WriteAsync($"\n\n[Sistemsel Hata Oluştu: {ex.Message}]", ct);
            }
        });
    }
}
