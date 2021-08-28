using MediatR;
using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class PurgeRecycledCommand : IRequest
    {
    }

    public class PurgeRecycledCommandHandler : IRequestHandler<PurgeRecycledCommand>
    {
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;
        private readonly IRepository<PostEntity> _postRepo;

        public PurgeRecycledCommandHandler(IBlogAudit audit, IBlogCache cache, IRepository<PostEntity> postRepo)
        {
            _audit = audit;
            _cache = cache;
            _postRepo = postRepo;
        }

        public async Task<Unit> Handle(PurgeRecycledCommand request, CancellationToken cancellationToken)
        {
            var spec = new PostSpec(true);
            var posts = await _postRepo.GetAsync(spec);
            await _postRepo.DeleteAsync(posts);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.EmptyRecycleBin, "Emptied Recycle Bin.");

            foreach (var guid in posts.Select(p => p.Id))
            {
                _cache.Remove(CacheDivision.Post, guid.ToString());
            }

            return Unit.Value;
        }
    }
}
