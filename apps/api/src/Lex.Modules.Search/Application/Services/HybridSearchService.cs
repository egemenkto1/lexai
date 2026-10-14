using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Lex.Modules.Search.Domain.Gateways;
using Lex.Modules.Search.Domain.Models;
using Lex.Modules.Search.Domain.Services;
using Lex.Modules.Search.Domain.ValueObjects;

namespace Lex.Modules.Search.Application.Services;

public sealed class HybridSearchService
{
    private readonly IEmbeddingGateway _embeddingGateway;
    private readonly IFullTextSearchGateway _fullTextGateway;
    private readonly IVectorSearchGateway _vectorGateway;
    private readonly RrfFusionService _rrfFusionService;
    private readonly ILogger<HybridSearchService> _logger;

    public HybridSearchService(
        IEmbeddingGateway embeddingGateway, 
        IFullTextSearchGateway fullTextGateway, 
        IVectorSearchGateway vectorGateway, 
        RrfFusionService rrfFusionService,
        ILogger<HybridSearchService> logger)
    {
        _embeddingGateway = embeddingGateway;
        _fullTextGateway = fullTextGateway;
        _vectorGateway = vectorGateway;
        _rrfFusionService = rrfFusionService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResultItem>> SearchAsync(string queryText, SearchFilterSet filters, Pagination pagination, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 [HYBRID] Arama başlatıldı. Sorgu: '{Query}'", queryText);
        var sw = Stopwatch.StartNew();

        // 1. Soruyu Vektöre Çevir
        var embedding = await _embeddingGateway.EmbedQueryAsync(queryText, cancellationToken);
        _logger.LogInformation("✅ [TEI] Vektörleştirme tamamlandı. Süre: {Ms}ms", sw.ElapsedMilliseconds);

        int fetchCount = 60; 
        sw.Restart();

        // 2. İki arama motoruna aynı anda (Paralel) sorgu at
        var textTask = _fullTextGateway.SearchAsync(queryText, filters, fetchCount, cancellationToken);
        var vectorTask = _vectorGateway.SearchAsync(embedding, filters, fetchCount, cancellationToken);

        try
        {
            // SİHİRLİ DOKUNUŞ: Sonsuza kadar beklemek YOK! 10 Saniyede cevap gelmezse sistemi iptal et (Timeout)
            await Task.WhenAll(textTask, vectorTask).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            _logger.LogInformation("✅ [MOTORLAR] Qdrant ve OpenSearch aramaları başarıyla tamamlandı. Süre: {Ms}ms", sw.ElapsedMilliseconds);
        }
        catch (TimeoutException)
        {
            _logger.LogError("❌ [TIMEOUT] Qdrant veya OpenSearch 10 saniye içinde cevap veremedi! Makine şu an veri indekslemekten boğulmuş durumda.");
            throw new Exception("Sistem şu an 640.000 kararı işliyor ve çok yoğun. Lütfen birkaç dakika sonra tekrar deneyin.");
        }

        sw.Restart();
        // 3. RRF formülü ile sonuçları harmanla
        var fusedResults = _rrfFusionService.Fuse(textTask.Result, vectorTask.Result, pagination.Take);
        _logger.LogInformation("✅ [RRF] Skorlar birleştirildi. Süre: {Ms}ms", sw.ElapsedMilliseconds);

        return fusedResults;
    }
}
