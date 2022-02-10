using Moonglade.Caching;

namespace Moonglade.Core.PostFeature;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : IRequest;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand>
{
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IBlogCache _cache;

    public DeletePostCommandHandler(IRepository<PostEntity> postRepo, IBlogCache cache)
    {
        _postRepo = postRepo;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var (guid, softDelete) = request;
        var post = await _postRepo.GetAsync(guid);
        if (null == post) return Unit.Value;

        if (softDelete)
        {
            post.IsDeleted = true;
            await _postRepo.UpdateAsync(post);
        }
        else
        {
            await _postRepo.DeleteAsync(post);
        }

        _cache.Remove(CacheDivision.Post, guid.ToString());
        return Unit.Value;
    }
}