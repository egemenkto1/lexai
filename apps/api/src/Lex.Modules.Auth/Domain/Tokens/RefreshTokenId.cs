using Lex.SharedKernel;

namespace Lex.Modules.Auth.Domain.Tokens;

public sealed class RefreshTokenId : ValueObject
{
    public Guid Value { get; }

    public RefreshTokenId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RefreshTokenId cannot be empty", nameof(value));
        Value = value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
