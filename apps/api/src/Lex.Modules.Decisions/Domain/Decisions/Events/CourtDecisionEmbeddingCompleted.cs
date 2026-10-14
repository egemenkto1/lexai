using Lex.SharedKernel;
using Lex.Modules.Decisions.Domain.ValueObjects;

namespace Lex.Modules.Decisions.Domain.Decisions.Events;

public record CourtDecisionEmbeddingCompleted(Guid EventId, DateTimeOffset OccurredOn, DecisionId DecisionId) : IAuditableEvent
{
    public static CourtDecisionEmbeddingCompleted Create(DecisionId decisionId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, decisionId);
}
