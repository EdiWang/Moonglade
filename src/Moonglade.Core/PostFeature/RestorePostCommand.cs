using Moonglade.CacheAside.InMemory;

namespace Moonglade.Core.PostFeature;

public record RestorePostCommand(Guid Id) : IRequest;

public class RestorePostCommandHandler : IRequestHandler<RestorePostCommand>
{
    private readonly IRepository<PostEntity> _repo;
    private readonly IBlogCache _cache;

    public RestorePostCommandHandler(IRepository<PostEntity> repo, IBlogCache cache)
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

        _cache.Remove(CachePartition.Post, request.Id.ToString());
    }
}