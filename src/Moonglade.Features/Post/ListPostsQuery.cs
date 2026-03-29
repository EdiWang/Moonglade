using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public class ListPostsQuery(int pageSize, int pageIndex, Guid? catId = null)
    : IQuery<List<PostDigest>>
{
    public int PageSize { get; set; } = pageSize;

    public int PageIndex { get; set; } = pageIndex;

    public Guid? CatId { get; set; } = catId;
}

public class ListPostsQueryHandler(BlogDbContext db) : IQueryHandler<ListPostsQuery, List<PostDigest>>
{
    public async Task<List<PostDigest>> HandleAsync(ListPostsQuery request, CancellationToken ct)
    {
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var startRow = (request.PageIndex - 1) * request.PageSize;

        var posts = await db.Post
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.PostStatus == PostStatus.Published &&
                        (request.CatId == null || p.PostCategory.Select(c => c.CategoryId).Contains(request.CatId.Value)))
            .OrderByDescending(p => p.PubDateUtc)
            .Skip(startRow)
            .Take(request.PageSize)
            .SelectToDigest()
            .ToListAsync(ct);

        return posts;
    }
}
