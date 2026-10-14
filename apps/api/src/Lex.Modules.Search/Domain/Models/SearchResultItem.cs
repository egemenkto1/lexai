namespace Lex.Modules.Search.Domain.Models;

public record SearchResultItem(
    Guid DecisionId, 
    string ChamberCode, 
    string CaseNumber, 
    string DecisionNumber, 
    DateOnly DecisionDate, 
    string? Snippet, 
    double RrfScore);
