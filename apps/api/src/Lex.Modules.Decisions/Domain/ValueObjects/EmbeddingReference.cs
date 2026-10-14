using Lex.SharedKernel;

namespace Lex.Modules.Decisions.Domain.ValueObjects;

public sealed class EmbeddingReference : ValueObject
{
    public Guid QdrantPointId { get; }
    public string Model { get; }
    public int Version { get; }

    public EmbeddingReference(Guid qdrantPointId, string model = "BAAI/bge-m3", int version = 1)
    {
        QdrantPointId = qdrantPointId;
        Model = model;
        Version = version;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return QdrantPointId;
        yield return Model;
        yield return Version;
    }
}
