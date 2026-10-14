using Lex.SharedKernel;

namespace Lex.Modules.Auth.Domain.Users.Events;

public record UserRegistered(Guid EventId, DateTimeOffset OccurredOn, UserId UserId, Email Email) : IAuditableEvent
{
    public static UserRegistered Create(UserId userId, Email email) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, userId, email);
}
