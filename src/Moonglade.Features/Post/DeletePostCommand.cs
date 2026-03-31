using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : ICommand;

public class DeletePostCommandHandler(
    BlogDbContext db,
    ILogger<DeletePostCommandHandler> logger
    ) : ICommandHandler<DeletePostCommand>
{
    public async Task HandleAsync(DeletePostCommand request, CancellationToken ct)
    {
        var (guid, softDelete) = request;
        var post = await db.Post.FindAsync([guid], ct);
        if (null == post) return;

        if (softDelete)
        {
            post.IsDeleted = true;
        }
        else
        {
            db.Post.Remove(post);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Post {PostId} deleted", guid);
    }
}