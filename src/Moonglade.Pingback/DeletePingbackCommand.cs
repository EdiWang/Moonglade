using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Pingback;

public record DeletePingbackCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler(MoongladeRepository<PingbackEntity> repo) : IRequestHandler<DeletePingbackCommand>
{
    public Task Handle(DeletePingbackCommand request, CancellationToken ct) => repo.DeleteAsync(request.Id, ct);
}