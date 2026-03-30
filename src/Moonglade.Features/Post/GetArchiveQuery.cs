using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Post;

public record GetArchiveQuery : IQuery<List<Archive>>;

public class GetArchiveQueryHandler(BlogDbContext dbContext) : IQueryHandler<GetArchiveQuery, List<Archive>>
{
    public async Task<List<Archive>> HandleAsync(GetArchiveQuery request, CancellationToken ct)
    {
        var query = dbContext.Post
            .AsNoTracking()
            .Where(p => p.PostStatus == PostStatus.Published && !p.IsDeleted);

        if (!await query.AnyAsync(ct))
        {
            return [];
        }

        var list = await query
            .GroupBy(post => new { post.PubDateUtc!.Value.Year, post.PubDateUtc.Value.Month })
            .Select(p => new Archive(p.Key.Year, p.Key.Month, p.Count()))
            .ToListAsync(ct);

        return list;
    }
}