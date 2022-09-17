using Moonglade.Caching;

namespace Moonglade.Core.PostFeature;

public record RestorePostCommand(Guid Id) : IRequest;

public class RestorePostCommandHandler : AsyncRequestHandler<RestorePostCommand>
{
    private readonly IRepository<PostEntity> _repo;
    private readonly IBlogCache _cache;

    public RestorePostCommandHandler(IRepository<PostEntity> repo, IBlogCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    protected override async Task Handle(RestorePostCommand request, CancellationToken ct)
    {
        var pp = await _repo.GetAsync(request.Id, ct);
        if (null == pp) return;

        pp.IsDeleted = false;
        await _repo.UpdateAsync(pp, ct);

        _cache.Remove(CacheDivision.Post, request.Id.ToString());
    }
}