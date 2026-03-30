using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Post;

public record GetPostByIdQuery(Guid Id) : IQuery<PostEntity>;

public class GetPostByIdQueryHandler(BlogDbContext db) : IQueryHandler<GetPostByIdQuery, PostEntity>
{
    public async Task<PostEntity> HandleAsync(GetPostByIdQuery request, CancellationToken ct)
    {
        var post = await db.Post
            .AsNoTracking()
            .Include(p => p.Tags)
            .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        return post;
    }
}