using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record PublishPostCommand(Guid Id) : ICommand;

public class PublishPostCommandHandler(
    BlogDbContext db,
    ILogger<PublishPostCommandHandler> logger,
    IEventMediator eventMediator
    ) : ICommandHandler<PublishPostCommand>
{
    public async Task HandleAsync(PublishPostCommand request, CancellationToken ct)
    {
        var post = await db.Post.FindAsync([request.Id], ct);
        if (post == null) return;

        var utcNow = DateTime.UtcNow;

        post.PostStatus = PostStatus.Published;
        post.PubDateUtc = utcNow;
        post.ScheduledPublishTimeUtc = null;
        post.LastModifiedUtc = utcNow;
        post.RouteLink = UrlHelper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), post.Slug);

        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Post [{PostId}] Published", request.Id);
    }
}