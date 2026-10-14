using Lex.SharedKernel;
using Lex.Modules.Decisions.Domain.ValueObjects;

namespace Lex.Modules.Decisions.Domain.Decisions.Events;

public record CourtDecisionEmbeddingFailed(Guid EventId, DateTimeOffset OccurredOn, DecisionId DecisionId, string Reason) : IAuditableEvent
{
    public static CourtDecisionEmbeddingFailed Create(DecisionId decisionId, string reason) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, decisionId, reason);
}
