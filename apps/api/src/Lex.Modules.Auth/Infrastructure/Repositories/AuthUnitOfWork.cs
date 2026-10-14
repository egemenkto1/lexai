using Lex.Modules.Auth.Domain.Repositories;
using Lex.Modules.Auth.Infrastructure.Data;

namespace Lex.Modules.Auth.Infrastructure.Repositories;

internal sealed class AuthUnitOfWork : IAuthUnitOfWork
{
    private readonly AuthDbContext _dbContext;

    public AuthUnitOfWork(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
