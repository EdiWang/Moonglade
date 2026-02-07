using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Post;

public record GetPostViewQuery(Guid PostId) : IQuery<PostViewEntity>;

public class GetPostViewQueryHandler(IRepositoryBase<PostViewEntity> repo) : IQueryHandler<GetPostViewQuery, PostViewEntity>
{
    public Task<PostViewEntity> HandleAsync(GetPostViewQuery request, CancellationToken ct) => repo.GetByIdAsync(request.PostId, ct);
}