using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Core.PostFeature;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : ICommand;

public class DeletePostCommandHandler(
    MoongladeRepository<PostEntity> repo,
    ICacheAside cache,
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

        cache.Remove(BlogCachePartition.Post.ToString(), guid.ToString());

        logger.LogInformation("Post {PostId} deleted", guid);
    }
}