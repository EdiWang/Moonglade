using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record RestorePostCommand(Guid Id) : ICommand;

public class RestorePostCommandHandler(
    BlogDbContext db,
    ILogger<RestorePostCommandHandler> logger) : ICommandHandler<RestorePostCommand>
{
    public async Task HandleAsync(RestorePostCommand request, CancellationToken ct)
    {
        var post = await db.Post.FindAsync([request.Id], ct);
        if (post == null) return;

        post.IsDeleted = false;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Post [{PostId}] restored", request.Id);
    }
}