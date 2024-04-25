using Edi.CacheAside.InMemory;
using Moonglade.Data;
using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record PurgeRecycledCommand : IRequest;

public class PurgeRecycledCommandHandler(ICacheAside cache, MoongladeRepository<PostEntity> repo) : IRequestHandler<PurgeRecycledCommand>
{
    public async Task Handle(PurgeRecycledCommand request, CancellationToken ct)
    {
        var spec = new PostByDeletionFlagSpec(true);
        var posts = await repo.ListAsync(spec, ct);
        await repo.DeleteRangeAsync(posts, ct);

        foreach (var guid in posts.Select(p => p.Id))
        {
            cache.Remove(BlogCachePartition.Post.ToString(), guid.ToString());
        }
    }
}