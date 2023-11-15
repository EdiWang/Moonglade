using Edi.CacheAside.InMemory;
using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record PurgeRecycledCommand : IRequest;

public class PurgeRecycledCommandHandler(ICacheAside cache, IRepository<PostEntity> repo) : IRequestHandler<PurgeRecycledCommand>
{
    public async Task Handle(PurgeRecycledCommand request, CancellationToken ct)
    {
        var spec = new PostSpec(true);
        var posts = await repo.ListAsync(spec);
        await repo.DeleteAsync(posts, ct);

        foreach (var guid in posts.Select(p => p.Id))
        {
            cache.Remove(BlogCachePartition.Post.ToString(), guid.ToString());
        }
    }
}