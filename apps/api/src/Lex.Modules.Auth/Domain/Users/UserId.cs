using Lex.SharedKernel;

namespace Lex.Modules.Auth.Domain.Users;

public sealed class UserId : ValueObject
{
    public Guid Value { get; }

    public UserId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(value));
        Value = value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
