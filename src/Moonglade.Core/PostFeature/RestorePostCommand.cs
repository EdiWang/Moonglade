using Edi.CacheAside.InMemory;

namespace Moonglade.Core.PostFeature;

public record RestorePostCommand(Guid Id) : IRequest;

public class RestorePostCommandHandler(IRepository<PostEntity> repo, ICacheAside cache) : IRequestHandler<RestorePostCommand>
{
    public async Task Handle(RestorePostCommand request, CancellationToken ct)
    {
        var pp = await repo.GetAsync(request.Id, ct);
        if (null == pp) return;

        pp.IsDeleted = false;
        await repo.UpdateAsync(pp, ct);

        cache.Remove(BlogCachePartition.Post.ToString(), request.Id.ToString());
    }
}