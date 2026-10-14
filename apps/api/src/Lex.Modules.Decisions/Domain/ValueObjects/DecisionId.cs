using Lex.SharedKernel;

namespace Lex.Modules.Decisions.Domain.ValueObjects;

public sealed class DecisionId : ValueObject
{
    public Guid Value { get; }
    public DecisionId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("DecisionId cannot be empty");
        Value = value;
    }
    public override IEnumerable<object> GetEqualityComponents() { yield return Value; }
}
