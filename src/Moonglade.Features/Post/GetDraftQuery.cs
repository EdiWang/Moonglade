using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Post;

public record GetDraftQuery(Guid Id) : IQuery<PostEntity>;

public class GetDraftQueryHandler(BlogDbContext db) : IQueryHandler<GetDraftQuery, PostEntity>
{
    public async Task<PostEntity> HandleAsync(GetDraftQuery request, CancellationToken ct)
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