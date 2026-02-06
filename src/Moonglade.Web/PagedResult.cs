namespace Moonglade.Web;

public class PagedResult<T>(IEnumerable<T> items, int pageNumber, int pageSize, int totalItemCount)
{
    public IReadOnlyList<T> Items { get; } = [.. items];
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;
    public int TotalItemCount { get; } = totalItemCount;

    public int PageCount => TotalItemCount > 0 ? (int)Math.Ceiling(TotalItemCount / (double)PageSize) : 0;
    public int Count => Items.Count;
}

public record PagerModel(int PageNumber, int PageCount, int MaxPageNumbers = 10);
