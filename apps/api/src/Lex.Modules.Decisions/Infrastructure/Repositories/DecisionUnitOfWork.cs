using Lex.Modules.Decisions.Domain.Repositories;
using Lex.Modules.Decisions.Infrastructure.Data;

namespace Lex.Modules.Decisions.Infrastructure.Repositories;

internal sealed class DecisionUnitOfWork : IDecisionUnitOfWork
{
    private readonly DecisionsDbContext _dbContext;

    public DecisionUnitOfWork(DecisionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
