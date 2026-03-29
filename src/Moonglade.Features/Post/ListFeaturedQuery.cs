using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IQuery<List<PostDigest>>;

public class ListFeaturedQueryHandler(BlogDbContext db) : IQueryHandler<ListFeaturedQuery, List<PostDigest>>
{
    public async Task<List<PostDigest>> HandleAsync(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var startRow = (pageIndex - 1) * pageSize;

        var posts = await db.Post
            .AsNoTracking()
            .Where(p => p.IsFeatured && !p.IsDeleted && p.PostStatus == PostStatus.Published)
            .OrderByDescending(p => p.PubDateUtc)
            .Skip(startRow)
            .Take(pageSize)
            .SelectToDigest()
            .ToListAsync(ct);

        return posts;
    }
}