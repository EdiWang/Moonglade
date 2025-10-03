using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Webmention;

public record ClearMentionsCommand : ICommand;

public class ClearMentionsCommandHandler(MoongladeRepository<MentionEntity> repo) : ICommandHandler<ClearMentionsCommand>
{
    public Task HandleAsync(ClearMentionsCommand request, CancellationToken ct) => repo.Clear(ct);
}