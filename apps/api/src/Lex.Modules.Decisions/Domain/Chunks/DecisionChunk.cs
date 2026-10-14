using Lex.SharedKernel;
using Lex.Modules.Decisions.Domain.ValueObjects;

namespace Lex.Modules.Decisions.Domain.Chunks;

public sealed class DecisionChunk : AggregateRoot<ChunkId>
{
    public DecisionId DecisionId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string ChunkText { get; private set; }
    public ChunkType Type { get; private set; }
    public int TokenCount { get; private set; }
    public EmbeddingReference? EmbeddingRef { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private DecisionChunk() { } // EF Core

    private DecisionChunk(ChunkId id, DecisionId decisionId, int chunkIndex, string text, ChunkType type, int tokenCount) 
        : base(id)
    {
        DecisionId = decisionId;
        ChunkIndex = chunkIndex;
        ChunkText = text;
        Type = type;
        TokenCount = tokenCount;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static DecisionChunk Create(DecisionId decisionId, int chunkIndex, string text, ChunkType type, int tokenCount)
    {
        return new DecisionChunk(new ChunkId(Guid.NewGuid()), decisionId, chunkIndex, text, type, tokenCount);
    }

    public void AssignEmbedding(EmbeddingReference reference)
    {
        EmbeddingRef = reference;
    }
}
