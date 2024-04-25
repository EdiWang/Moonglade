using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Pingback;

public record GetPingbacksQuery : IRequest<List<PingbackEntity>>;

public class GetPingbacksQueryHandler(MoongladeRepository<PingbackEntity> repo) : IRequestHandler<GetPingbacksQuery, List<PingbackEntity>>
{
    public Task<List<PingbackEntity>> Handle(GetPingbacksQuery request, CancellationToken ct) => repo.ListNoTrackingAsync(ct);
}