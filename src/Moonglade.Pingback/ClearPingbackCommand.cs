using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public class ClearPingbackCommand : IRequest
{
}

public class ClearPingbackCommandHandler : IRequestHandler<ClearPingbackCommand>
{
    private readonly IRepository<PingbackEntity> _pingbackRepo;

    public ClearPingbackCommandHandler(IRepository<PingbackEntity> pingbackRepo)
    {
        _pingbackRepo = pingbackRepo;
    }

    public async Task<Unit> Handle(ClearPingbackCommand request, CancellationToken cancellationToken)
    {
        await _pingbackRepo.ExecuteSqlRawAsync("DELETE FROM Pingback");
        return Unit.Value;
    }
}