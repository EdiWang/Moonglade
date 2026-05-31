using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Post;

public class ListPostSegmentQuery(PostStatus postStatus, int offset, int pageSize, PostFilter filter = null)
    : IQuery<(List<PostSegment> Posts, int TotalRows)>
{
    public PostStatus PostStatus { get; set; } = postStatus;

    public int Offset { get; set; } = offset;

    public int PageSize { get; set; } = pageSize;

    public PostFilter Filter { get; set; } = filter ?? new();
}

public class ListPostSegmentQueryHandler(BlogDbContext db) :
    IQueryHandler<ListPostSegmentQuery, (List<PostSegment> Posts, int TotalRows)>
{
    public async Task<(List<PostSegment> Posts, int TotalRows)> HandleAsync(ListPostSegmentQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
        }

        if (request.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.Offset)} can not be less than 0, current value: {request.Offset}.");
        }

        var query = db.Post
            .AsNoTracking()
            .FilterByStatus(request.PostStatus);

        if (!string.IsNullOrWhiteSpace(request.Filter.Title))
        {
            query = query.Where(p => p.Title.Contains(request.Filter.Title));
        }

        if (!string.IsNullOrWhiteSpace(request.Filter.ContentAbstract))
        {
            query = query.Where(p => p.ContentAbstract.Contains(request.Filter.ContentAbstract));
        }

        if (!string.IsNullOrWhiteSpace(request.Filter.Tag))
        {
            query = query.Where(p => p.Tags.Any(t =>
                t.DisplayName.Contains(request.Filter.Tag) ||
                t.NormalizedName.Contains(request.Filter.Tag)));
        }

        var totalRows = await query.CountAsync(ct);

        var orderedQuery = request.Filter.SortDescending
            ? query.OrderByDescending(p => p.PubDateUtc)
            : query.OrderBy(p => p.PubDateUtc);

        var posts = await orderedQuery
            .Skip(request.Offset)
            .Take(request.PageSize)
            .SelectToSegment()
            .ToListAsync(ct);

        return (posts, totalRows);
    }
}