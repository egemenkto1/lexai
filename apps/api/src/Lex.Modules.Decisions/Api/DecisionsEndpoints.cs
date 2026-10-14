using Lex.Modules.Decisions.Application.Services;
using Lex.Modules.Decisions.Domain.ValueObjects; // Enumlar için bunu ekledik
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Lex.Modules.Decisions.Api;

public static class DecisionsEndpoints
{
    public static void MapDecisionsEndpoints(this IEndpointRouteBuilder app)
    {
        // JWT Auth'u ileride ekleyeceğiz, MVP testleri için şimdilik kapıyı açık bırakıyoruz.
        var group = app.MapGroup("/api/decisions").WithTags("Decisions");

        // Python Scraper'ın vuracağı ana kapı
        group.MapPost("/", async ([FromBody] RegisterDecisionRequest request, [FromServices] DecisionRegistrationService service, CancellationToken ct) =>
        {
            var result = await service.RegisterDecisionAsync(
                CourtType.Yargitay, // <-- SİHİRLİ DOKUNUŞ: Mahkeme türünü açıkça belirttik!
                request.ChamberCode,
                request.CaseNumber,
                request.DecisionNumber,
                request.DecisionDate,
                request.RawTextStorageKey,
                null, // ChamberName şimdilik opsiyonel geçiliyor
                ct);

            return result.IsSuccess 
                ? Results.Ok(new { DecisionId = result.Value.Value }) 
                : Results.BadRequest(result.Error);
        });
    }
}

// Scraper'dan gelecek JSON yükünün formatı
public record RegisterDecisionRequest(
    string ChamberCode,
    string CaseNumber,
    string DecisionNumber,
    DateOnly DecisionDate,
    string RawTextStorageKey);
