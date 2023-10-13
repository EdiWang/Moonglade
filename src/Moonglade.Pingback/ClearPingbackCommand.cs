using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record ClearPingbackCommand : IRequest;

public class ClearPingbackCommandHandler(IRepository<PingbackEntity> repo) : IRequestHandler<ClearPingbackCommand>
{
    public Task Handle(ClearPingbackCommand request, CancellationToken ct) => repo.Clear(ct);
}