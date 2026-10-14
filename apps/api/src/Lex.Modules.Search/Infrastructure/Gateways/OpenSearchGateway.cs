using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenSearch.Client;
using Lex.Modules.Search.Domain.Gateways;
using Lex.Modules.Search.Domain.Models;
using Lex.Modules.Search.Domain.ValueObjects;

namespace Lex.Modules.Search.Infrastructure.Gateways;

public sealed class OpenSearchGateway : IFullTextSearchGateway
{
    private readonly IOpenSearchClient _client;
    private const string IndexName = "court_decisions";

    public OpenSearchGateway(IOpenSearchClient client)
    {
        _client = client;
    }

    // --- SİHİRLİ DOKUNUŞ 1: OpenSearch Verilerini Okumak İçin Özel Sınıf ---
    private class OpenSearchDecisionDoc
    {
        public string id { get; set; }
        public string decision_text { get; set; }
        public int year { get; set; }
        public int category_code { get; set; }
    }

    // --- MEVCUT: HUKUK SÖZLÜĞÜ VE İNDEKS KURULUMU ---
    public async Task InitializeIndexAsync(CancellationToken cancellationToken = default)
    {
        var existsResponse = await _client.Indices.ExistsAsync(IndexName, ct: cancellationToken);
        
        if (!existsResponse.Exists)
        {
            var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Settings(s => s
                    .Analysis(a => a
                        .TokenFilters(tf => tf
                            .Synonym("hukuk_synonyms", stf => stf
                                .Synonyms(
                                    "iş akdi, iş sözleşmesi, hizmet akdi",
                                    "yargıtay, temyiz mahkemesi",
                                    "maddi tazminat, bedensel zarar",
                                    "kıdem, ihbar, fesih",
                                    "ceza, mahkumiyet"
                                )
                            )
                        )
                        .Analyzers(an => an
                            .Custom("hukuk_analyzer", ca => ca
                                .Tokenizer("standard") 
                                .Filters("lowercase", "hukuk_synonyms") 
                            )
                        )
                    )
                )
                .Map<dynamic>(m => m
                    .Properties(p => p
                        .Text(t => t
                            .Name("decision_text")
                            .Analyzer("hukuk_analyzer")
                        )
                        .Keyword(k => k.Name("chamber_code"))
                        .Number(n => n.Name("year").Type(NumberType.Integer))
                        .Number(n => n.Name("category_code").Type(NumberType.Integer))
                        .Date(d => d.Name("indexed_at"))
                    )
                )
            );

            if (!createResponse.IsValid)
            {
                throw new Exception($"OpenSearch Index oluşturulamadı: {createResponse.DebugInformation}");
            }
        }
    }

    // --- MEVCUT ARAMA METODU (GÜNCELLENDİ) ---
    public async Task<IReadOnlyList<FullTextHit>> SearchAsync(string query, SearchFilterSet filters, int topK, CancellationToken cancellationToken = default)
    {
        // SİHİRLİ DOKUNUŞ: Sınıf (Class) kullanmıyoruz. Veriyi esnek Sözlük (Dictionary) olarak alıyoruz.
        // Bu sayede C# "Bu harf büyüktü, küçüktü" diye naz yapamaz!
        var response = await _client.SearchAsync<Dictionary<string, object>>(s => s
            .Index(IndexName)
            .Query(q => q
                .Bool(b => 
                {
                    b.Must(m => m
                        .Match(mt => mt
                            .Field("decision_text")
                            .Query(query)
                        )
                    );

                    b.Should(sh => sh
                        .Terms(t => t
                            .Field("category_code")
                            .Terms(3, 4) 
                            .Boost(2.5)  
                        )
                    );

                    var filterQueries = new List<Func<QueryContainerDescriptor<Dictionary<string, object>>, QueryContainer>>();

                    if (filters != null)
                    {
                        if (filters.StartDate.HasValue || filters.EndDate.HasValue)
                        {
                            filterQueries.Add(fq => fq.Range(r => {
                                r.Field("year");
                                if (filters.StartDate.HasValue) r.GreaterThanOrEquals(filters.StartDate.Value.Year);
                                if (filters.EndDate.HasValue) r.LessThanOrEquals(filters.EndDate.Value.Year);
                                return r;
                            }));
                        }

                        if (filters.ChamberCodes != null && filters.ChamberCodes.Any())
                        {
                            filterQueries.Add(fq => fq.Terms(t => t.Field("chamber_code.keyword").Terms(filters.ChamberCodes)));
                        }
                    }

                    if (filterQueries.Any())
                    {
                        b.Filter(filterQueries.ToArray());
                    }

                    return b;
                })
            )
            .Highlight(h => h
                .Fields(f => f
                    .Field("decision_text")
                )
            )
            .Size(topK),
            cancellationToken
        );

        if (!response.IsValid)
        {
            throw new Exception($"OpenSearch arama hatası: {response.DebugInformation}");
        }

        var hits = new List<FullTextHit>();
        foreach (var hit in response.Hits)
        {
            Guid.TryParse(hit.Id, out Guid decisionId);
            
            // DİREKT SÖZLÜKTEN METNİ KOPARIYORUZ
            string fullText = "";
            if (hit.Source != null && hit.Source.TryGetValue("decision_text", out var textObj))
            {
                fullText = textObj?.ToString() ?? "";
            }

            string snippet = "";
            if (hit.Highlight != null && hit.Highlight.TryGetValue("decision_text", out var highlights))
            {
                snippet = string.Join(" ... ", highlights);
            }
            else if (!string.IsNullOrWhiteSpace(fullText))
            {
                // Vurgu yoksa bile metnin ilk 400 karakterini zorla veriyoruz ki LLM kafayı yemesin!
                snippet = fullText.Length > 400 ? fullText.Substring(0, 400) + "..." : fullText;
            }

            hits.Add(new FullTextHit(
                DecisionId: decisionId,
                ChamberCode: "Bilinmiyor", 
                CaseNumber: "Bilinmiyor",
                DecisionNumber: "Bilinmiyor",
                DecisionDate: DateOnly.FromDateTime(DateTime.UtcNow),
                Snippet: snippet,
                Bm25Score: hit.Score ?? 0
            ));
        }

        return hits;
    }

    // --- MEVCUT YAZMA METODU ---
    public async Task IndexDecisionAsync(string decisionIdStr, string text, int year, int categoryCode, CancellationToken cancellationToken = default)
    {
        var document = new
        {
            id = decisionIdStr,
            decision_text = text,
            year = year,
            category_code = categoryCode,
            indexed_at = DateTime.UtcNow.ToString("o")
        };

        var response = await _client.IndexAsync(document, idx => idx.Index(IndexName).Id(decisionIdStr), cancellationToken);

        if (!response.IsValid)
        {
            throw new Exception($"OpenSearch indexleme hatası: {response.DebugInformation}");
        }
    }
}
