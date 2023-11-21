using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record GetPingbacksQuery : IRequest<IReadOnlyList<PingbackEntity>>;

public class GetPingbacksQueryHandler(IRepository<PingbackEntity> repo) : IRequestHandler<GetPingbacksQuery, IReadOnlyList<PingbackEntity>>
{
    public Task<IReadOnlyList<PingbackEntity>> Handle(GetPingbacksQuery request, CancellationToken ct) => repo.ListAsync(ct);
}