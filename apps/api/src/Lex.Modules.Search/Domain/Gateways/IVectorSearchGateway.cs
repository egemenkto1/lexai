using Lex.Modules.Search.Domain.Models;
using Lex.Modules.Search.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lex.Modules.Search.Domain.Gateways;

public interface IVectorSearchGateway
{
    // Mevcut Okuma Metodu
    Task<IReadOnlyList<VectorHit>> SearchAsync(float[] queryEmbedding, SearchFilterSet filters, int topK, CancellationToken cancellationToken = default);
    
    // YENİ EKLENEN YAZMA METODU (Qdrant'a chunk kaydetmek için)
    Task UpsertChunkAsync(string chunkId, string decisionId, string text, float[] embedding, int year, int categoryCode, CancellationToken cancellationToken = default);
}
