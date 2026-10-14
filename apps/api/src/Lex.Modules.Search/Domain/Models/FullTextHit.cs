namespace Lex.Modules.Search.Domain.Models;

public record FullTextHit(Guid DecisionId, string ChamberCode, string CaseNumber, string DecisionNumber, DateOnly DecisionDate, string Snippet, double Bm25Score);
