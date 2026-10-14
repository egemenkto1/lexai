using Lex.SharedKernel;

namespace Lex.Modules.Search.Domain.ValueObjects;

public sealed class Pagination : ValueObject
{
    public int Page { get; }
    public int PageSize { get; }

    public Pagination(int page = 1, int pageSize = 20)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize is < 1 or > 50 ? 20 : pageSize;
    }

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Page;
        yield return PageSize;
    }
}
