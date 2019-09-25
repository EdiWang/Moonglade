using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.SyndicationFeedGenerator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class SyndicationService : MoongladeService
    {
        private readonly IBlogConfig _blogConfig;

        private readonly string _baseUrl;

        private readonly IRepository<CategoryEntity> _categoryRepository;

        private readonly IRepository<PostEntity> _postRepository;

        public SyndicationService(
            ILogger<SyndicationService> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            IHttpContextAccessor httpContextAccessor,
            IRepository<CategoryEntity> categoryRepository,
            IRepository<PostEntity> postRepository) : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _categoryRepository = categoryRepository;
            _postRepository = postRepository;

            var acc = httpContextAccessor;
            _baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";
        }

        public async Task RefreshRssFilesForCategoryAsync(string categoryName)
        {
            Logger.LogInformation($"Start refreshing RSS feed for category {categoryName}.");
            var cat = await _categoryRepository.GetAsync(c => c.Title == categoryName);
            if (null != cat)
            {
                var itemCollection = await GetPostsAsFeedItemsAsync(cat.Id);

                var rw = new SyndicationFeedGenerator
                {
                    HostUrl = _baseUrl,
                    HeadTitle = _blogConfig.FeedSettings.RssTitle,
                    HeadDescription = _blogConfig.FeedSettings.RssDescription,
                    Copyright = _blogConfig.FeedSettings.RssCopyright,
                    Generator = _blogConfig.FeedSettings.RssGeneratorName,
                    FeedItemCollection = itemCollection,
                    TrackBackUrl = _baseUrl,
                    MaxContentLength = 0
                };

                await rw.WriteRss20FileAsync($@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed\posts-category-{categoryName}.xml");
                Logger.LogInformation($"Finished refreshing RSS feed for category {categoryName}.");
            }
            else
            {
                Logger.LogWarning($"Trying to refresh rss feed for category {categoryName} but {categoryName} is not found.");
            }
        }

        public async Task RefreshFeedFileForPostAsync(bool isAtom)
        {
            Logger.LogInformation("Start refreshing feed for posts.");
            var itemCollection = await GetPostsAsFeedItemsAsync();

            var rw = new SyndicationFeedGenerator
            {
                HostUrl = _baseUrl,
                HeadTitle = _blogConfig.FeedSettings.RssTitle,
                HeadDescription = _blogConfig.FeedSettings.RssDescription,
                Copyright = _blogConfig.FeedSettings.RssCopyright,
                Generator = _blogConfig.FeedSettings.RssGeneratorName,
                FeedItemCollection = itemCollection,
                TrackBackUrl = _baseUrl,
                MaxContentLength = 0
            };

            if (isAtom)
            {
                Logger.LogInformation("Writing ATOM file.");
                await rw.WriteAtom10FileAsync($@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed\posts-atom.xml");
            }
            else
            {
                Logger.LogInformation("Writing RSS file.");
                await rw.WriteRss20FileAsync($@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed\posts.xml");
            }

            Logger.LogInformation("Finished writing feed for posts.");
        }

        private Task<IReadOnlyList<SimpleFeedItem>> GetPostsAsFeedItemsAsync(Guid? categoryId = null)
        {
            Logger.LogInformation($"{nameof(GetPostsAsFeedItemsAsync)} - {nameof(categoryId)}: {categoryId}");

            int? top = null;
            if (_blogConfig.FeedSettings.RssItemCount != 0)
            {
                top = _blogConfig.FeedSettings.RssItemCount;
            }

            var postSpec = new PostSpec(categoryId, top);
            return _postRepository.SelectAsync(postSpec, p => p.PostPublish.PubDateUtc != null ? new SimpleFeedItem
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                PubDateUtc = p.PostPublish.PubDateUtc.Value,
                Description = p.ContentAbstract,
                Link = $"{_baseUrl}/post/{p.PostPublish.PubDateUtc.Value.Year}/{p.PostPublish.PubDateUtc.Value.Month}/{p.PostPublish.PubDateUtc.Value.Day}/{p.Slug}",
                Author = _blogConfig.FeedSettings.AuthorName,
                AuthorEmail = _blogConfig.EmailSettings.AdminEmail,
                Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToList()
            } : null);
        }
    }
}
