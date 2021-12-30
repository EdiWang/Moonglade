using MediatR;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public class PurgeRecycledCommand : IRequest
{
}

public class PurgeRecycledCommandHandler : IRequestHandler<PurgeRecycledCommand>
{
    private readonly IBlogCache _cache;
    private readonly IRepository<PostEntity> _postRepo;

    public PurgeRecycledCommandHandler(IBlogCache cache, IRepository<PostEntity> postRepo)
    {
        _cache = cache;
        _postRepo = postRepo;
    }

    public async Task<Unit> Handle(PurgeRecycledCommand request, CancellationToken cancellationToken)
    {
        var spec = new PostSpec(true);
        var posts = await _postRepo.GetAsync(spec);
        await _postRepo.DeleteAsync(posts);

        foreach (var guid in posts.Select(p => p.Id))
        {
            _cache.Remove(CacheDivision.Post, guid.ToString());
        }

        return Unit.Value;
    }
}