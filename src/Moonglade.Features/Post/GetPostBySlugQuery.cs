using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Post;

public record GetPostBySlugQuery(string RouteLink) : IQuery<PostEntity>;

public class GetPostBySlugQueryHandler(BlogDbContext db)
    : IQueryHandler<GetPostBySlugQuery, PostEntity>
{
    public async Task<PostEntity> HandleAsync(GetPostBySlugQuery request, CancellationToken ct)
    {
        var post = await db.Post
            .AsNoTracking()
            .Include(p => p.Comments)
            .Include(p => p.Tags)
            .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.RouteLink == request.RouteLink
                                   && p.PostStatus == PostStatus.Published
                                   && !p.IsDeleted, ct);

        return post;
    }
}