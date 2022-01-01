using MediatR;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.PostFeature;

public class RestorePostCommand : IRequest
{
    public RestorePostCommand(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}

public class RestorePostCommandHandler : IRequestHandler<RestorePostCommand>
{
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IBlogCache _cache;

    public RestorePostCommandHandler(IRepository<PostEntity> postRepo, IBlogCache cache)
    {
        _postRepo = postRepo;
        _cache = cache;
    }

    public async Task<Unit> Handle(RestorePostCommand request, CancellationToken cancellationToken)
    {
        var pp = await _postRepo.GetAsync(request.Id);
        if (null == pp) return Unit.Value;

        pp.IsDeleted = false;
        await _postRepo.UpdateAsync(pp);

        _cache.Remove(CacheDivision.Post, request.Id.ToString());
        return Unit.Value;
    }
}