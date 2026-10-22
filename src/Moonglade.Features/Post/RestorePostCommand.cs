using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record RestorePostCommand(Guid Id) : ICommand;

public class RestorePostCommandHandler(
    BlogDbContext db,
    ILogger<RestorePostCommandHandler> logger,
    IEventMediator eventMediator) : ICommandHandler<RestorePostCommand>
{
    public async Task HandleAsync(RestorePostCommand request, CancellationToken ct)
    {
        var post = await db.Post.FindAsync([request.Id], ct);
        if (post == null) return;

        post.IsDeleted = false;
        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Post [{PostId}] restored", request.Id);
    }
}