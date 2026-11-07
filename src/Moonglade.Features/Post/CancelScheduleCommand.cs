using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record CancelScheduleCommand(Guid Id) : ICommand;

public class CancelScheduleCommandHandler(
    BlogDbContext db,
    ILogger<CancelScheduleCommandHandler> logger,
    IEventMediator eventMediator
    ) : ICommandHandler<CancelScheduleCommand>
{
    public async Task HandleAsync(CancelScheduleCommand request, CancellationToken ct)
    {
        var post = await db.Post.FindAsync([request.Id], ct);
        if (post == null) return;

        if (post.PostStatus != PostStatus.Scheduled)
        {
            logger.LogWarning("Post [{PostId}] is not scheduled, cannot cancel schedule", request.Id);
            return;
        }

        PostEntityHelper.SetStatus(post, PostStatus.Draft);
        post.LastModifiedUtc = DateTime.UtcNow;

        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Post [{PostId}] schedule canceled", request.Id);
    }
}
