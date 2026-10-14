namespace Lex.Modules.Decisions.Domain.Repositories;

public interface IDecisionUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
