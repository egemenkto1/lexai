using System.Text.RegularExpressions;
using Lex.SharedKernel;

namespace Lex.Modules.Auth.Domain.Users;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(Error.Validation("Email.Empty", "Email cannot be empty."));

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (!emailRegex.IsMatch(email))
            return Result.Failure<Email>(Error.Validation("Email.InvalidFormat", "Email format is invalid."));

        return Result.Success(new Email(email.ToLowerInvariant()));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
