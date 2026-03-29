using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Webmention;

public record DeleteMentionsCommand(List<Guid> Ids) : ICommand;

public class DeleteMentionsCommandHandler(BlogDbContext db) : ICommandHandler<DeleteMentionsCommand>
{
    public async Task HandleAsync(DeleteMentionsCommand request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
        {
            return;
        }

        var entities = await db.Mention
            .Where(m => request.Ids.Contains(m.Id))
            .ToListAsync(ct);

        if (entities.Count != 0)
        {
            db.Mention.RemoveRange(entities);
            await db.SaveChangesAsync(ct);
        }
    }
}
