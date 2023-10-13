using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback;

public record DeletePingbackCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler(IRepository<PingbackEntity> repo) : IRequestHandler<DeletePingbackCommand>
{
    public Task Handle(DeletePingbackCommand request, CancellationToken ct) => repo.DeleteAsync(request.Id, ct);
}