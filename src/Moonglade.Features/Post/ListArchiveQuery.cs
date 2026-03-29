using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Post;

public record ListArchiveQuery(int Year, int? Month = null) : IQuery<List<PostDigest>>;

public class ListArchiveQueryHandler(BlogDbContext db) : IQueryHandler<ListArchiveQuery, List<PostDigest>>
{
    public async Task<List<PostDigest>> HandleAsync(ListArchiveQuery request, CancellationToken ct)
    {
        var month = request.Month.GetValueOrDefault();

        var list = await db.Post
            .AsNoTracking()
            .Where(p => p.PubDateUtc.Value.Year == request.Year &&
                        (month == 0 || p.PubDateUtc.Value.Month == month))
            .Where(p => p.PostStatus == PostStatus.Published && !p.IsDeleted)
            .OrderByDescending(p => p.PubDateUtc)
            .SelectToDigest()
            .ToListAsync(ct);

        return list;
    }
}