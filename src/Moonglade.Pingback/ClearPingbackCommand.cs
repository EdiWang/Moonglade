using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record ClearPingbackCommand : IRequest;

public class ClearPingbackCommandHandler : AsyncRequestHandler<ClearPingbackCommand>
{
    private readonly IRepository<PingbackEntity> _repo;

    public ClearPingbackCommandHandler(IRepository<PingbackEntity> repo) => _repo = repo;

    protected override Task Handle(ClearPingbackCommand request, CancellationToken ct) =>
        _repo.Clear(ct);
}