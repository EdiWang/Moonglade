using MediatR;
using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.PostFeature
{
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
        private readonly IBlogAudit _audit;

        public RestorePostCommandHandler(IRepository<PostEntity> postRepo, IBlogCache cache, IBlogAudit audit)
        {
            _postRepo = postRepo;
            _cache = cache;
            _audit = audit;
        }

        public async Task<Unit> Handle(RestorePostCommand request, CancellationToken cancellationToken)
        {
            var pp = await _postRepo.GetAsync(request.Id);
            if (null == pp) return Unit.Value;

            pp.IsDeleted = false;
            await _postRepo.UpdateAsync(pp);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostRestored, $"Post restored, id: {request.Id}");

            _cache.Remove(CacheDivision.Post, request.Id.ToString());
            return Unit.Value;
        }
    }
}
