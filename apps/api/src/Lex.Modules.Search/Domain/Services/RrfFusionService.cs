using Lex.Modules.Search.Domain.Models;

namespace Lex.Modules.Search.Domain.Services;

public sealed class RrfFusionService
{
    // RRF formülü için sabitleme katsayısı (genelde 60 kullanılır)
    private const int K = 60;
    
    // Mimari kararımız: Vektör aramaya %70, BM25 tam metin aramasına %30 ağırlık.
    private const double Bm25Weight = 0.3;
    private const double VectorWeight = 0.7;

    public IReadOnlyList<SearchResultItem> Fuse(IReadOnlyList<FullTextHit> textHits, IReadOnlyList<VectorHit> vectorHits, int take)
    {
        var scores = new Dictionary<Guid, (double TotalScore, FullTextHit? TextHit, VectorHit? VectorHit)>();

        // 1. BM25 Skorlarını hesapla ve sözlüğe at
        for (int i = 0; i < textHits.Count; i++)
        {
            var hit = textHits[i];
            var rank = i + 1;
            var rrfScore = 1.0 / (K + rank) * Bm25Weight;

            scores[hit.DecisionId] = (rrfScore, hit, null);
        }

        // 2. Vektör Skorlarını hesapla ve sözlükteki verilerle birleştir
        for (int i = 0; i < vectorHits.Count; i++)
        {
            var hit = vectorHits[i];
            var rank = i + 1;
            var rrfScore = 1.0 / (K + rank) * VectorWeight;

            if (scores.TryGetValue(hit.DecisionId, out var existing))
            {
                // Karar hem BM25'te hem Vektörde çıktıysa skorları topla (Zirveye oynar)
                scores[hit.DecisionId] = (existing.TotalScore + rrfScore, existing.TextHit, hit);
            }
            else
            {
                scores[hit.DecisionId] = (rrfScore, null, hit);
            }
        }

        // 3. Skorlara göre büyükten küçüğe sırala ve DTO'ya (SearchResultItem) çevir
        var fusedResults = scores
            .OrderByDescending(x => x.Value.TotalScore)
            .Take(take)
            .Select(x => 
            {
                var id = x.Key;
                var textHit = x.Value.TextHit;
                var vectorHit = x.Value.VectorHit;
                var score = x.Value.TotalScore;

                // EFSANEVİ FALLBACK: OpenSearch kelimeyi bulamadıysa (veya Snippet boşsa), 
                // Qdrant'ın anlamsal olarak bulduğu o saf Türkçe metni kullan!
                string finalSnippet = !string.IsNullOrWhiteSpace(textHit?.Snippet) 
                    ? textHit.Snippet 
                    : vectorHit?.Text ?? "";

                return new SearchResultItem(
                    DecisionId: id,
                    ChamberCode: textHit?.ChamberCode ?? "BİLİNMİYOR",
                    CaseNumber: textHit?.CaseNumber ?? "-",
                    DecisionNumber: textHit?.DecisionNumber ?? "-",
                    DecisionDate: textHit?.DecisionDate ?? DateOnly.MinValue,
                    Snippet: finalSnippet, // Artık ASLA boş gitmeyecek!
                    RrfScore: score
                );
            })
            .ToList();

        return fusedResults;
    }
}
