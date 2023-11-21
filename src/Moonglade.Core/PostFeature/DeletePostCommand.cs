using Edi.CacheAside.InMemory;

namespace Moonglade.Core.PostFeature;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : IRequest;

public class DeletePostCommandHandler(IRepository<PostEntity> repo, ICacheAside cache) : IRequestHandler<DeletePostCommand>
{
    public async Task Handle(DeletePostCommand request, CancellationToken ct)
    {
        var (guid, softDelete) = request;
        var post = await repo.GetAsync(guid, ct);
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
    }
}