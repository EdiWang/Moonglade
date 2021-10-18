using MediatR;
using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.PostFeature
{
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
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;

        public DeletePostCommandHandler(IRepository<PostEntity> postRepo, IBlogAudit audit, IBlogCache cache)
        {
            _postRepo = postRepo;
            _audit = audit;
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
                await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostRecycled, $"Post '{request.Id}' moved to Recycle Bin.");
            }
            else
            {
                await _postRepo.DeleteAsync(post);
                await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostDeleted, $"Post '{request.Id}' deleted from Recycle Bin.");
            }

            _cache.Remove(CacheDivision.Post, request.Id.ToString());
            return Unit.Value;
        }
    }
}
