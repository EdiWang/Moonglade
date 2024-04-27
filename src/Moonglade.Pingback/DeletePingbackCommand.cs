using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Pingback;

public record DeletePingbackCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler(MoongladeRepository<PingbackEntity> repo) : IRequestHandler<DeletePingbackCommand>
{
    public async Task Handle(DeletePingbackCommand request, CancellationToken ct)
    {
        var pingback = await repo.GetByIdAsync(request.Id, ct);
        if (pingback != null)
        {
            await repo.DeleteAsync(pingback, ct);
        }
    }
}