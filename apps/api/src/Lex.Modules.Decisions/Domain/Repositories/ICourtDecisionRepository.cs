using Lex.Modules.Decisions.Domain.Decisions;
using Lex.Modules.Decisions.Domain.ValueObjects;

namespace Lex.Modules.Decisions.Domain.Repositories;

public interface ICourtDecisionRepository
{
    Task<CourtDecision?> GetByIdAsync(DecisionId id, CancellationToken cancellationToken = default);
    Task<CourtDecision?> GetByCaseIdentifierAsync(CaseIdentifier identifier, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(CaseIdentifier identifier, CancellationToken cancellationToken = default);
    void Add(CourtDecision decision);
    void Update(CourtDecision decision);
}
