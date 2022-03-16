using Moonglade.Caching;

namespace Moonglade.Core.PostFeature;

public record RestorePostCommand(Guid Id) : IRequest;

public class RestorePostCommandHandler : AsyncRequestHandler<RestorePostCommand>
{
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IBlogCache _cache;

    public RestorePostCommandHandler(IRepository<PostEntity> postRepo, IBlogCache cache)
    {
        _postRepo = postRepo;
        _cache = cache;
    }

    protected override async Task Handle(RestorePostCommand request, CancellationToken cancellationToken)
    {
        var pp = await _postRepo.GetAsync(request.Id);
        if (null == pp) return;

        pp.IsDeleted = false;
        await _postRepo.UpdateAsync(pp);

        _cache.Remove(CacheDivision.Post, request.Id.ToString());
    }
}