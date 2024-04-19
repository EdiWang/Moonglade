using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record GetPingbacksQuery : IRequest<List<PingbackEntity>>;

public class GetPingbacksQueryHandler(IRepository<PingbackEntity> repo) : IRequestHandler<GetPingbacksQuery, List<PingbackEntity>>
{
    public Task<List<PingbackEntity>> Handle(GetPingbacksQuery request, CancellationToken ct) => repo.ListAsync(ct);
}