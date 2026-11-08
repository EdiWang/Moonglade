using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record PublishScheduledPostCommand : ICommand<int>;

public class PublishScheduledPostCommandHandler(
    BlogDbContext db,
    TimeProvider timeProvider,
    IEventMediator eventMediator,
    ILogger<PublishScheduledPostCommandHandler> logger) :
    ICommandHandler<PublishScheduledPostCommand, int>
{
    public async Task<int> HandleAsync(PublishScheduledPostCommand request, CancellationToken ct)
    {
        var dueCutoffUtc = timeProvider.GetUtcNow().UtcDateTime;
        var scheduledPosts = await db.Post
            .Where(p => p.PostStatus == PostStatus.Scheduled && !p.IsDeleted && p.ScheduledPublishTimeUtc <= dueCutoffUtc)
            .ToListAsync(ct);

        if (scheduledPosts.Count == 0)
        {
            return 0;
        }

        var publishedTimeUtc = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var post in scheduledPosts)
        {
            PostEntityHelper.SetStatus(post, PostStatus.Published, publishedTimeUtc);
        }

        var rows = await db.SaveChangesAsync(ct);
        if (rows > 0)
        {
            var publications = Array.AsReadOnly(scheduledPosts
                .Select(post => new ScheduledPostPublication(post.Id, post.Title, publishedTimeUtc))
                .ToArray());
            try
            {
                await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
            }
            finally
            {
                // A cache failure after commit must not suppress the independent activity handoff.
                try
                {
                    await eventMediator.PublishAsync(new ScheduledPostsPublishedEvent(publications), cancellationToken: CancellationToken.None);
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        "Scheduled posts were published, but the publication handoff failed for {PostCount} posts. Failure type: {FailureType}",
                        publications.Count, exception.GetType().Name);
                }
            }
        }

        return rows;
    }
}
