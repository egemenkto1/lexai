using Lex.SharedKernel;

namespace Lex.Modules.Search.Domain.ValueObjects;

public sealed class SearchFilterSet : ValueObject
{
    public IReadOnlyList<string>? ChamberCodes { get; }
    public DateOnly? StartDate { get; }
    public DateOnly? EndDate { get; }

    public SearchFilterSet(IReadOnlyList<string>? chamberCodes = null, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            throw new ArgumentException("Start date cannot be after end date.");

        ChamberCodes = chamberCodes;
        StartDate = startDate;
        EndDate = endDate;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        if (ChamberCodes != null)
            foreach (var code in ChamberCodes.OrderBy(x => x)) yield return code;
            
        if (StartDate.HasValue) yield return StartDate.Value;
        if (EndDate.HasValue) yield return EndDate.Value;
    }
}
