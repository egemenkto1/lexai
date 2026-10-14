using Lex.SharedKernel;
using Lex.Modules.Auth.Domain.Users;

namespace Lex.Modules.Auth.Domain.Tokens;

public sealed class RefreshToken : AggregateRoot<RefreshTokenId>
{
    public UserId UserId { get; private set; }
    public TokenHash TokenHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RefreshToken() { } // EF Core için

    private RefreshToken(RefreshTokenId id, UserId userId, TokenHash tokenHash, DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static RefreshToken Issue(UserId userId, TokenHash tokenHash, DateTimeOffset expiresAt)
    {
        return new RefreshToken(new RefreshTokenId(Guid.NewGuid()), userId, tokenHash, expiresAt);
    }

    public bool IsValid(DateTimeOffset now)
    {
        return RevokedAt == null && ExpiresAt > now;
    }

    public Result Revoke(DateTimeOffset at)
    {
        if (RevokedAt != null)
            return Result.Failure(Error.Conflict("Token.AlreadyRevoked", "Refresh token is already revoked."));

        RevokedAt = at;
        return Result.Success();
    }
}
