using Lex.Modules.Auth.Domain.Tokens;

namespace Lex.Modules.Auth.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(TokenHash hash, CancellationToken cancellationToken = default);
    void Add(RefreshToken token);
    void Update(RefreshToken token);
}
