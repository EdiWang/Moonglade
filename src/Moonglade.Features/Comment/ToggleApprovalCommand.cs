using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Comment;

public record ToggleApprovalCommand(Guid[] CommentIds) : ICommand;

public class ToggleApprovalCommandHandler(
    BlogDbContext db,
    ILogger<ToggleApprovalCommandHandler> logger) : ICommandHandler<ToggleApprovalCommand>
{
    public async Task HandleAsync(ToggleApprovalCommand request, CancellationToken ct)
    {
        var comments = await db.Comment
            .Where(c => request.CommentIds.Contains(c.Id))
            .ToListAsync(ct);

        foreach (var cmt in comments)
        {
            cmt.IsApproved = !cmt.IsApproved;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Toggled approval status for {Count} comment(s)", comments.Count);
    }
}