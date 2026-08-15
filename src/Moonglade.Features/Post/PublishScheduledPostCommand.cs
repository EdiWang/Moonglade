using LiteBus.Commands.Abstractions;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record PublishScheduledPostCommand : ICommand<int>;

public class PublishScheduledPostCommandHandler(BlogDbContext db, TimeProvider timeProvider) :
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
            post.PostStatus = PostStatus.Published;
            post.PubDateUtc = publishedTimeUtc;
            post.ScheduledPublishTimeUtc = null;
            post.RouteLink = UrlHelper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), post.Slug);
        }

        return await db.SaveChangesAsync(ct);
    }
}
