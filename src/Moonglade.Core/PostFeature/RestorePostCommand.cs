using Edi.CacheAside.InMemory;

namespace Moonglade.Core.PostFeature;

public record RestorePostCommand(Guid Id) : IRequest;

public class RestorePostCommandHandler : IRequestHandler<RestorePostCommand>
{
    private readonly IRepository<PostEntity> _repo;
    private readonly ICacheAside _cache;

    public RestorePostCommandHandler(IRepository<PostEntity> repo, ICacheAside cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task Handle(RestorePostCommand request, CancellationToken ct)
    {
        var pp = await _repo.GetAsync(request.Id, ct);
        if (null == pp) return;

        pp.IsDeleted = false;
        await _repo.UpdateAsync(pp, ct);

        _cache.Remove(BlogCachePartition.Post.ToString(), request.Id.ToString());
    }
}