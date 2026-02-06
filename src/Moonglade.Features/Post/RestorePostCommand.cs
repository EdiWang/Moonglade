using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record RestorePostCommand(Guid Id) : ICommand;

public class RestorePostCommandHandler(
    IRepositoryBase<PostEntity> repo,
    ILogger<RestorePostCommandHandler> logger) : ICommandHandler<RestorePostCommand>
{
    public async Task HandleAsync(RestorePostCommand request, CancellationToken ct)
    {
        var post = await repo.GetByIdAsync(request.Id, ct);
        if (null == post) return;

        post.IsDeleted = false;
        await repo.UpdateAsync(post, ct);

        logger.LogInformation("Post [{PostId}] restored", request.Id);
    }
}