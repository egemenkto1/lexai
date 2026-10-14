using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Lex.Modules.Decisions.Domain.Decisions.Events;
using Lex.Modules.Decisions.Domain.Repositories;
using Lex.Modules.Search.Domain.Gateways;
using Lex.Modules.Decisions.Domain.ValueObjects; // EmbeddingStatus için

namespace Lex.Modules.Decisions.Application.EventHandlers;

internal sealed class CourtDecisionRegisteredEventHandler : INotificationHandler<CourtDecisionRegistered>
{
    private readonly ICourtDecisionRepository _repository;
    private readonly IDecisionUnitOfWork _unitOfWork;
    private readonly IObjectStorageGateway _objectStorage;
    private readonly IEmbeddingGateway _embeddingGateway;
    private readonly IVectorSearchGateway _vectorSearchGateway;
    private readonly IFullTextSearchGateway _fullTextSearchGateway;
    private readonly ILogger<CourtDecisionRegisteredEventHandler> _logger;

    public CourtDecisionRegisteredEventHandler(
        ICourtDecisionRepository repository,
        IDecisionUnitOfWork unitOfWork,
        IObjectStorageGateway objectStorage,
        IEmbeddingGateway embeddingGateway,
        IVectorSearchGateway vectorSearchGateway,
        IFullTextSearchGateway fullTextSearchGateway,
        ILogger<CourtDecisionRegisteredEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _objectStorage = objectStorage;
        _embeddingGateway = embeddingGateway;
        _vectorSearchGateway = vectorSearchGateway;
        _fullTextSearchGateway = fullTextSearchGateway;
        _logger = logger;
    }

    public async Task Handle(CourtDecisionRegistered notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"🧠 AI Pipeline Tetiklendi! ID: {notification.DecisionId.Value}");

        // 1. Kararı Bul
        var decision = await _repository.GetByIdAsync(notification.DecisionId, cancellationToken);
        if (decision is null) return;

        // --- SİHİRLİ DOKUNUŞ 1: IDEMPOTENCY (HAFIZA) ---
        // Eğer RabbitMQ bağlantı kopması vs. yüzünden mesajı tekrar yolladıysa ve biz bunu zaten yaptıysak, atla!
        if (decision.EmbeddingStatus == EmbeddingStatus.Done)
        {
            _logger.LogWarning($"⚠️ IDEMPOTENCY KORUMASI: Karar (ID: {decision.Id.Value}) zaten işlenmiş. Tekrar edilmiyor.");
            return;
        }

        // Embedding işleminin başladığını DB'ye bildir (Eğer Pending veya Failed ise burası çalışır)
        decision.MarkEmbeddingProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // 2. Metni İndir
            var rawText = await _objectStorage.DownloadTextAsync("decisions", decision.RawTextLocation.Key, cancellationToken);
            if (string.IsNullOrWhiteSpace(rawText)) throw new Exception("MinIO'dan boş metin döndü.");

            // 3. Chunking (Parçalama)
            List<string> chunks = CreateChunks(rawText, 1500, 300);
            
            // 4. OpenSearch'e Bütün Metni Gönder
            // (OpenSearch, aynı ID ile gelirse otomatik olarak eskisini ezer/günceller. Çift kayıt yaratmaz, güvenlidir.)
            await _fullTextSearchGateway.IndexDecisionAsync(
                decisionId: decision.Id.Value.ToString(),
                text: rawText,
                year: decision.DecisionYear,
                categoryCode: (int)decision.Category,
                cancellationToken: cancellationToken);

            // --- SİHİRLİ DOKUNUŞ 2: PARALEL İŞLEME (TURBO MOTOR) ---
            var chunkObjects = chunks.Select((text, index) => new { Text = text, Index = index }).ToList();
            
            // TEI sunucusunu boğmamak için aynı anda maksimum 5 istek (Thread) gönderiyoruz
            var parallelOptions = new ParallelOptions 
            { 
                MaxDegreeOfParallelism = 5, 
                CancellationToken = cancellationToken 
            };

            await Parallel.ForEachAsync(chunkObjects, parallelOptions, async (chunkData, ct) =>
            {
                // Metni bge-m3 ile Vektöre Çevir
                float[] embedding = await _embeddingGateway.EmbedQueryAsync(chunkData.Text, ct);
                
                string chunkId = $"{decision.Id.Value}_{chunkData.Index}";

                // Vektörü Qdrant'a Kaydet
                await _vectorSearchGateway.UpsertChunkAsync(
                    chunkId: chunkId,
                    decisionId: decision.Id.Value.ToString(),
                    text: chunkData.Text,
                    embedding: embedding,
                    year: decision.DecisionYear,
                    categoryCode: (int)decision.Category,
                    cancellationToken: ct);
            });

            // 6. Başarı Durumunu Kaydet
            decision.MarkEmbeddingCompleted(DateTimeOffset.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation($"✅ BAŞARILI: {chunks.Count} parça paralel olarak vektörleştirildi ve indekslendi!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ HATA: Karar (ID: {notification.DecisionId.Value}) işlenirken patladı!");
            decision.MarkEmbeddingFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private List<string> CreateChunks(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        int currentIndex = 0;
        while (currentIndex < text.Length)
        {
            int remaining = text.Length - currentIndex;
            int length = Math.Min(chunkSize, remaining);
            
            if (length == chunkSize && currentIndex + length < text.Length)
            {
                int lastSpace = text.LastIndexOf(' ', currentIndex + length - 1, chunkSize / 3);
                if (lastSpace > currentIndex) length = lastSpace - currentIndex;
            }

            chunks.Add(text.Substring(currentIndex, length).Trim());
            currentIndex += length - overlap;
            if (length - overlap <= 0) break;
        }
        return chunks;
    }
}
