using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Pingback;

public record ClearMentionsCommand : IRequest;

public class ClearPingbackCommandHandler(MoongladeRepository<MentionEntity> repo) : IRequestHandler<ClearMentionsCommand>
{
    public Task Handle(ClearMentionsCommand request, CancellationToken ct) => repo.Clear(ct);
}