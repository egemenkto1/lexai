using Lex.Modules.Auth.Domain.Repositories;
using Lex.Modules.Auth.Domain.Tokens;
using Lex.Modules.Auth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Modules.Auth.Infrastructure.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _dbContext;

    public RefreshTokenRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(TokenHash hash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, cancellationToken);
    }

    public void Add(RefreshToken token)
    {
        _dbContext.RefreshTokens.Add(token);
    }

    public void Update(RefreshToken token)
    {
        _dbContext.RefreshTokens.Update(token);
    }
}
