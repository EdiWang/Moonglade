using Edi.CacheAside.InMemory;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record EmptyRecycleBinCommand : IRequest;

public class EmptyRecycleBinCommandHandler(ICacheAside cache, MoongladeRepository<PostEntity> repo) : IRequestHandler<EmptyRecycleBinCommand>
{
    public async Task Handle(EmptyRecycleBinCommand request, CancellationToken ct)
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