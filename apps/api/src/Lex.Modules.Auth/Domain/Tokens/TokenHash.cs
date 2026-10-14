using Lex.SharedKernel;

namespace Lex.Modules.Auth.Domain.Tokens;

public sealed class TokenHash : ValueObject
{
    public string Value { get; }

    public TokenHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Token hash cannot be empty", nameof(value));
        Value = value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
