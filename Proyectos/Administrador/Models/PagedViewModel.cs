namespace NewRich.Admin.Models;

public class PagerInfo
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));
    public int From => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int To => Math.Min(Page * PageSize, TotalCount);
}

public sealed class PagedViewModel<T> : PagerInfo
{
    public IReadOnlyList<T> Items { get; init; } = [];
}

public static class PagingHelper
{
    public static readonly int[] PageSizes = [5, 10, 15];

    public static int NormalizePageSize(int pageSize) =>
        PageSizes.Contains(pageSize) ? pageSize : 10;

    public static PagedViewModel<T> Paginate<T>(IReadOnlyList<T> source, int page, int pageSize)
    {
        pageSize = NormalizePageSize(pageSize);
        var total = source.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedViewModel<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}
