using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : ICommand;

public class DeletePostCommandHandler(
    IRepositoryBase<PostEntity> repo,
    ILogger<DeletePostCommandHandler> logger
    ) : ICommandHandler<DeletePostCommand>
{
    public async Task HandleAsync(DeletePostCommand request, CancellationToken ct)
    {
        var (guid, softDelete) = request;
        var post = await repo.GetByIdAsync(guid, ct);
        if (null == post) return;

        if (softDelete)
        {
            post.IsDeleted = true;
            await repo.UpdateAsync(post, ct);
        }
        else
        {
            await repo.DeleteAsync(post, ct);
        }

        logger.LogInformation("Post {PostId} deleted", guid);
    }
}