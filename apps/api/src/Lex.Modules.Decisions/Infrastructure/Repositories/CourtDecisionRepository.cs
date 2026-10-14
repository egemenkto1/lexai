using Lex.Modules.Decisions.Domain.Decisions;
using Lex.Modules.Decisions.Domain.Repositories;
using Lex.Modules.Decisions.Domain.ValueObjects;
using Lex.Modules.Decisions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lex.Modules.Decisions.Infrastructure.Repositories;

internal sealed class CourtDecisionRepository : ICourtDecisionRepository
{
    private readonly DecisionsDbContext _dbContext;

    public CourtDecisionRepository(DecisionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CourtDecision?> GetByIdAsync(DecisionId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CourtDecisions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<CourtDecision?> GetByCaseIdentifierAsync(CaseIdentifier identifier, CancellationToken cancellationToken = default)
    {
        // Nesne kıyaslaması yerine özellik (property) kıyaslaması yapıyoruz
        return await _dbContext.CourtDecisions.FirstOrDefaultAsync(x => 
            x.CaseIdentifier.ChamberCode == identifier.ChamberCode &&
            x.CaseIdentifier.CaseNumber == identifier.CaseNumber &&
            x.CaseIdentifier.DecisionNumber == identifier.DecisionNumber, cancellationToken);
    }

    public async Task<bool> ExistsAsync(CaseIdentifier identifier, CancellationToken cancellationToken = default)
    {
        // Nesne kıyaslaması yerine özellik (property) kıyaslaması yapıyoruz
        return await _dbContext.CourtDecisions.AnyAsync(x => 
            x.CaseIdentifier.ChamberCode == identifier.ChamberCode &&
            x.CaseIdentifier.CaseNumber == identifier.CaseNumber &&
            x.CaseIdentifier.DecisionNumber == identifier.DecisionNumber, cancellationToken);
    }

    public void Add(CourtDecision decision)
    {
        _dbContext.CourtDecisions.Add(decision);
    }

    public void Update(CourtDecision decision)
    {
        _dbContext.CourtDecisions.Update(decision);
    }
}
