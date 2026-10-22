using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : ICommand;

public class DeletePostCommandHandler(
    BlogDbContext db,
    ILogger<DeletePostCommandHandler> logger,
    IEventMediator eventMediator
    ) : ICommandHandler<DeletePostCommand>
{
    public async Task HandleAsync(DeletePostCommand request, CancellationToken ct)
    {
        var (guid, softDelete) = request;
        var post = await db.Post.FindAsync([guid], ct);
        if (post == null) return;

        if (softDelete)
        {
            post.IsDeleted = true;
        }
        else
        {
            db.Post.Remove(post);
        }

        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Post {PostId} deleted", guid);
    }
}