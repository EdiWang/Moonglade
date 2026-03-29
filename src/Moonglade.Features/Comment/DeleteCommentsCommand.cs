using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Comment;

public record DeleteCommentsCommand(Guid[] Ids) : ICommand;

public class DeleteCommentsCommandHandler(
    BlogDbContext db,
    ILogger<DeleteCommentsCommandHandler> logger) : ICommandHandler<DeleteCommentsCommand>
{
    public async Task HandleAsync(DeleteCommentsCommand request, CancellationToken ct)
    {
        var comments = await db.Comment
            .Include(c => c.Replies)
            .Where(c => request.Ids.Contains(c.Id))
            .ToListAsync(ct);

        db.Comment.RemoveRange(comments);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deleted {Count} comment(s)", comments.Count);
    }
}