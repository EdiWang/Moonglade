using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record DeletePingbackCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler : AsyncRequestHandler<DeletePingbackCommand>
{
    private readonly IRepository<PingbackEntity> _pingbackRepo;

    public DeletePingbackCommandHandler(IRepository<PingbackEntity> pingbackRepo) => _pingbackRepo = pingbackRepo;

    protected override Task Handle(DeletePingbackCommand request, CancellationToken cancellationToken) =>
        _pingbackRepo.DeleteAsync(request.Id, cancellationToken);
}