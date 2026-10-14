using System;

namespace Lex.Modules.Decisions.Domain.Decisions.Events;

// Python'un RabbitMQ'ya fırlatacağı o devasa JSON paketinin C# karşılığı
public record DecisionIngestedEvent(
    string ChamberCode,
    string CaseNumber,
    string DecisionNumber,
    DateOnly DecisionDate,
    string FullText, // O meşhur devasa karar metni
    string? Summary
);
