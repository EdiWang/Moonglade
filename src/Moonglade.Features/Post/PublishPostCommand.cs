using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

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

        PostEntityHelper.SetStatus(post, PostStatus.Published, utcNow);
        post.LastModifiedUtc = utcNow;

        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Post [{PostId}] Published", request.Id);
    }
}
