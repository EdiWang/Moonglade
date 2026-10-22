using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;

namespace Moonglade.Features.Post;

public record PublishScheduledPostCommand : ICommand<int>;

public class PublishScheduledPostCommandHandler(BlogDbContext db, TimeProvider timeProvider, IEventMediator eventMediator) :
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
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        return rows;
    }
}
