namespace Lex.SharedKernel;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}

public interface IAuditableEvent : IDomainEvent { }
