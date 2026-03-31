using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Post;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IQuery<List<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler(BlogDbContext db) : IQueryHandler<ListPostSegmentByStatusQuery, List<PostSegment>>
{
    public async Task<List<PostSegment>> HandleAsync(ListPostSegmentByStatusQuery request, CancellationToken ct)
    {
        return await db.Post
            .AsNoTracking()
            .FilterByStatus(request.Status)
            .SelectToSegment()
            .ToListAsync(ct);
    }
}