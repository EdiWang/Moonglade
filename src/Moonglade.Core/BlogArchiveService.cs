using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core
{
    public interface IBlogArchiveService
    {
        Task<IReadOnlyList<Archive>> ListAsync();
        Task<IReadOnlyList<PostDigest>> ListPostsAsync(int year, int month = 0);
    }

    public class BlogArchiveService : IBlogArchiveService
    {
        private readonly ILogger<BlogArchiveService> _logger;
        private readonly IRepository<PostEntity> _postRepo;

        public BlogArchiveService(
            ILogger<BlogArchiveService> logger,
            IRepository<PostEntity> postRepo)
        {
            _logger = logger;
            _postRepo = postRepo;
        }

        public async Task<IReadOnlyList<Archive>> ListAsync()
        {
            if (!_postRepo.Any(p => p.IsPublished && !p.IsDeleted))
            {
                return new List<Archive>();
            }

            var spec = new PostSpec(PostStatus.Published);
            var list = await _postRepo.SelectAsync(spec, post => new
            {
                post.PubDateUtc.Value.Year,
                post.PubDateUtc.Value.Month
            }, monthList => new Archive(
                monthList.Key.Year,
                monthList.Key.Month,
                monthList.Count()));

            return list;
        }

        public Task<IReadOnlyList<PostDigest>> ListPostsAsync(int year, int month = 0)
        {
            if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
            {
                _logger.LogError($"parameter '{nameof(year)}:{year}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            if (month is > 12 or < 0)
            {
                _logger.LogError($"parameter '{nameof(month)}:{month}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(month));
            }

            var spec = new PostSpec(year, month);
            var list = _postRepo.SelectAsync(spec, SharedSelectors.PostDigestSelector);
            return list;
        }
    }
}
