using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record DeletePingbackCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler : IRequestHandler<DeletePingbackCommand>
{
    private readonly IRepository<PingbackEntity> _pingbackRepo;

    public DeletePingbackCommandHandler(IRepository<PingbackEntity> pingbackRepo)
    {
        _pingbackRepo = pingbackRepo;
    }

    public async Task<Unit> Handle(DeletePingbackCommand request, CancellationToken cancellationToken)
    {
        await _pingbackRepo.DeleteAsync(request.Id);
        return Unit.Value;
    }
}