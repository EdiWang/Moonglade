using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Mention.Common;

public record ClearMentionsCommand : ICommand;

public class ClearPingbackCommandHandler(MoongladeRepository<MentionEntity> repo) : ICommandHandler<ClearMentionsCommand>
{
    public Task HandleAsync(ClearMentionsCommand request, CancellationToken ct) => repo.Clear(ct);
}