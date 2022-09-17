using Moonglade.Caching;

namespace Moonglade.Core.PostFeature;

public record DeletePostCommand(Guid Id, bool SoftDelete = false) : IRequest;

public class DeletePostCommandHandler : AsyncRequestHandler<DeletePostCommand>
{
    private readonly IRepository<PostEntity> _repo;
    private readonly IBlogCache _cache;

    public DeletePostCommandHandler(IRepository<PostEntity> repo, IBlogCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    protected override async Task Handle(DeletePostCommand request, CancellationToken ct)
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

        _cache.Remove(CacheDivision.Post, guid.ToString());
    }
}