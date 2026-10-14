namespace Lex.Modules.Auth.Domain.Repositories;

public interface IAuthUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
