using Lex.SharedKernel;

namespace Lex.Modules.Decisions.Domain.ValueObjects;

public sealed class ChunkId : ValueObject
{
    public Guid Value { get; }
    public ChunkId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("ChunkId cannot be empty");
        Value = value;
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
