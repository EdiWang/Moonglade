using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record GetPingbacksQuery : IRequest<IReadOnlyList<PingbackEntity>>;

public class GetPingbacksQueryHandler : IRequestHandler<GetPingbacksQuery, IReadOnlyList<PingbackEntity>>
{
    private readonly IRepository<PingbackEntity> _repo;

    public GetPingbacksQueryHandler(IRepository<PingbackEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<PingbackEntity>> Handle(GetPingbacksQuery request, CancellationToken ct) => _repo.ListAsync(ct);
}