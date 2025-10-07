using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features.PostFeature;

public record GetPostViewQuery(Guid PostId) : IQuery<PostViewEntity>;

public class GetPostViewQueryHandler(MoongladeRepository<PostViewEntity> repo) : IQueryHandler<GetPostViewQuery, PostViewEntity>
{
    public Task<PostViewEntity> HandleAsync(GetPostViewQuery request, CancellationToken ct) => repo.GetByIdAsync(request.PostId, ct);
}