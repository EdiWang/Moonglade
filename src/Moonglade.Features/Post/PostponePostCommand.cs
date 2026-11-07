using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record PostponePostCommand(Guid PostId, int Hours) : ICommand;

public class PostponePostCommandHandler(
    BlogDbContext db,
    ILogger<PostponePostCommandHandler> logger,
    IEventMediator eventMediator) : ICommandHandler<PostponePostCommand>
{
    public async Task HandleAsync(PostponePostCommand request, CancellationToken ct)
    {
        var post = await db.Post.FindAsync([request.PostId], ct);
        if (post == null)
        {
            logger.LogWarning("Post with ID {PostId} not found", request.PostId);
            return;
        }

        if (post.PostStatus == PostStatus.Scheduled && post.ScheduledPublishTimeUtc.HasValue)
        {
            post.ScheduledPublishTimeUtc = post.ScheduledPublishTimeUtc.Value.AddHours(request.Hours);
            if (await db.SaveChangesAsync(ct) > 0)
            {
                await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
            }

            logger.LogInformation("Post {PostId} postponed by {Hours} hour(s)", request.PostId, request.Hours);
        }
    }
}