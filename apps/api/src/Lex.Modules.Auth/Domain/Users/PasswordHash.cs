using Lex.SharedKernel;

namespace Lex.Modules.Auth.Domain.Users;

public sealed class PasswordHash : ValueObject
{
    public string Value { get; }

    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Password hash cannot be empty", nameof(value));
        Value = value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
