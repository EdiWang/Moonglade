using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Post;

public record GetPostViewQuery(Guid PostId) : IQuery<PostViewEntity>;

public class GetPostViewQueryHandler(BlogDbContext db) : IQueryHandler<GetPostViewQuery, PostViewEntity>
{
    public Task<PostViewEntity> HandleAsync(GetPostViewQuery request, CancellationToken ct) =>
        db.PostView.AsNoTracking().FirstOrDefaultAsync(pv => pv.PostId == request.PostId, ct);
}