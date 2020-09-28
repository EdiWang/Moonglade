using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class PostArchiveService : BlogService
    {
        private readonly IRepository<PostEntity> _postRepository;

        public PostArchiveService(
            ILogger<PostArchiveService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepository) : base(logger, settings)
        {
            _postRepository = postRepository;
        }

        public Task<Response<IReadOnlyList<Archive>>> ListAsync()
        {
            return TryExecuteAsync<IReadOnlyList<Archive>>(async () =>
            {
                if (!_postRepository.Any(p =>
                    p.IsPublished && !p.IsDeleted))
                    return new SuccessResponse<IReadOnlyList<Archive>>();

                var spec = new PostSpec(PostPublishStatus.Published);
                var list = await _postRepository.SelectAsync(spec, post => new
                {
                    post.PubDateUtc.Value.Year,
                    post.PubDateUtc.Value.Month
                }, monthList => new Archive(
                    monthList.Key.Year,
                    monthList.Key.Month,
                    monthList.Count()));

                return new SuccessResponse<IReadOnlyList<Archive>>(list);
            });
        }

        public async Task<IReadOnlyList<PostListEntry>> ListPostsAsync(int year, int month = 0)
        {
            if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
            {
                Logger.LogError($"parameter '{nameof(year)}:{year}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            if (month > 12 || month < 0)
            {
                Logger.LogError($"parameter '{nameof(month)}:{month}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(month));
            }

            var spec = new PostSpec(year, month);
            var list = await _postRepository.SelectAsync(spec, p => new PostListEntry
            {
                Title = p.Title,
                Slug = p.Slug,
                ContentAbstract = p.ContentAbstract,
                PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
                LangCode = p.ContentLanguageCode
            });
            return list;
        }
    }
}
