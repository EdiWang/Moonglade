using Edi.CacheAside.InMemory;

namespace Moonglade.Core.PostFeature;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : IRequest;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand>
{
    private readonly IRepository<PostEntity> _repo;
    private readonly ICacheAside _cache;

    public DeletePostCommandHandler(IRepository<PostEntity> repo, ICacheAside cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public async Task Handle(DeletePostCommand request, CancellationToken ct)
    {
        var (guid, softDelete) = request;
        var post = await _repo.GetAsync(guid, ct);
        if (null == post) return;

        if (softDelete)
        {
            post.IsDeleted = true;
            await _repo.UpdateAsync(post, ct);
        }
        else
        {
            await _repo.DeleteAsync(post, ct);
        }

        _cache.Remove(BlogCachePartition.Post.ToString(), guid.ToString());
    }
}