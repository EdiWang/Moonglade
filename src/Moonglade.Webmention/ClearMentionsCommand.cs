using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Webmention;

public record ClearMentionsCommand : ICommand;

public class ClearMentionsCommandHandler(BlogDbContext dbContext) : ICommandHandler<ClearMentionsCommand>
{
    public Task HandleAsync(ClearMentionsCommand request, CancellationToken ct) =>
        dbContext.Set<MentionEntity>().ExecuteDeleteAsync(ct);
}