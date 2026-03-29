using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Post;

public class ListPostSegmentQuery(PostStatus postStatus, int offset, int pageSize, string keyword = null)
    : IQuery<(List<PostSegment> Posts, int TotalRows)>
{
    public PostStatus PostStatus { get; set; } = postStatus;

    public int Offset { get; set; } = offset;

    public int PageSize { get; set; } = pageSize;

    public string Keyword { get; set; } = keyword;
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

        if (request.Keyword is not null)
        {
            query = query.Where(p => p.Title.Contains(request.Keyword));
        }

        var totalRows = await query.CountAsync(ct);

        var posts = await query
            .OrderByDescending(p => p.PubDateUtc)
            .Skip(request.Offset)
            .Take(request.PageSize)
            .SelectToSegment()
            .ToListAsync(ct);

        return (posts, totalRows);
    }
}