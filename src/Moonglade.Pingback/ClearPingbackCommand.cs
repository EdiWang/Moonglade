using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Pingback;

public record ClearPingbackCommand : IRequest;

public class ClearPingbackCommandHandler(MoongladeRepository<MentionEntity> repo) : IRequestHandler<ClearPingbackCommand>
{
    public Task Handle(ClearPingbackCommand request, CancellationToken ct) => repo.Clear(ct);
}