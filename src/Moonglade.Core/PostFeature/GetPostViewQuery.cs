using Moonglade.Data;

namespace Moonglade.Core.PostFeature;

public record GetPostViewQuery(Guid PostId) : IRequest<PostViewEntity>;

public class GetPostViewQueryHandler(MoongladeRepository<PostViewEntity> repo) : IRequestHandler<GetPostViewQuery, PostViewEntity>
{
    public Task<PostViewEntity> Handle(GetPostViewQuery request, CancellationToken ct) => repo.GetByIdAsync(request.PostId, ct);
}