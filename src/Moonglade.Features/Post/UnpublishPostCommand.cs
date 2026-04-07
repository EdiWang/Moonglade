using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record UnpublishPostCommand(Guid Id) : ICommand;

public class UnpublishPostCommandHandler(
    BlogDbContext db,
    ILogger<UnpublishPostCommandHandler> logger
    ) : ICommandHandler<UnpublishPostCommand>
{
    public async Task HandleAsync(UnpublishPostCommand request, CancellationToken ct)
    {
        var post = await db.Post.FindAsync([request.Id], ct);
        if (post == null) return;

        post.PostStatus = PostStatus.Draft;
        post.PubDateUtc = null;
        post.ScheduledPublishTimeUtc = null;
        post.RouteLink = null;
        post.LastModifiedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Post [{PostId}] unpublished", request.Id);
    }
}