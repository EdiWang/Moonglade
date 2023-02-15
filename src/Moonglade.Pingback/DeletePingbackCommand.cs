using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record DeletePingbackCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler : IRequestHandler<DeletePingbackCommand>
{
    private readonly IRepository<PingbackEntity> _repo;

    public DeletePingbackCommandHandler(IRepository<PingbackEntity> repo) => _repo = repo;

    public Task Handle(DeletePingbackCommand request, CancellationToken ct) => _repo.DeleteAsync(request.Id, ct);
}