using MediatR;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.PostFeature;

public class DeletePostCommand : IRequest
{
    public DeletePostCommand(Guid id, bool softDelete = false)
    {
        Id = id;
        SoftDelete = softDelete;
    }

    public Guid Id { get; set; }

    public bool SoftDelete { get; set; }
}

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
        var post = await _postRepo.GetAsync(request.Id);
        if (null == post) return Unit.Value;

        if (request.SoftDelete)
        {
            post.IsDeleted = true;
            await _postRepo.UpdateAsync(post);
        }
        else
        {
            await _postRepo.DeleteAsync(post);
        }

        _cache.Remove(CacheDivision.Post, request.Id.ToString());
        return Unit.Value;
    }
}