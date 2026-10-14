using System;
using MediatR;
using Lex.SharedKernel;
using Lex.Modules.Decisions.Domain.ValueObjects;

namespace Lex.Modules.Decisions.Domain.Decisions.Events;

// Hem senin DDD mimarine (IDomainEvent) hem de arka plan işçisine (INotification) uyumlu!
public record CourtDecisionRegistered(Guid EventId, DateTimeOffset OccurredOn, DecisionId DecisionId) : IDomainEvent, INotification
{
    public static CourtDecisionRegistered Create(DecisionId decisionId) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, decisionId);
}
