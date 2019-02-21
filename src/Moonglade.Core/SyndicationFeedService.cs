using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.SyndicationFeedGenerator;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class SyndicationFeedService : MoongladeService
    {
        private readonly BlogConfig _blogConfig;

        private readonly string _baseUrl;

        public SyndicationFeedService(
            MoongladeDbContext context,
            ILogger<SyndicationFeedService> logger,
            IOptions<AppSettings> settings,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService, 
            IHttpContextAccessor httpContextAccessor) : base(context, logger, settings)
        {
            _blogConfig = blogConfig;
            _blogConfig.GetConfiguration(blogConfigurationService);

            var acc = httpContextAccessor;
            _baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";
        }

        public async Task RefreshRssFilesForCategoryAsync(string categoryName)
        {
            Logger.LogInformation($"Start refreshing RSS feed for category {categoryName}.");
            var cat = Context.Category.FirstOrDefault(c => c.Title == categoryName);
            if (null != cat)
            {
                var itemCollection = GetPostsAsRssFeedItems(cat.Id);

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

                Logger.LogInformation($"Writing RSS file for category id {cat.Id}");
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
            List<SimpleFeedItem> itemCollection = GetPostsAsRssFeedItems();

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

        private List<SimpleFeedItem> GetPostsAsRssFeedItems(Guid? categoryId = null)
        {
            Logger.LogInformation($"{nameof(GetPostsAsRssFeedItems)} - {nameof(categoryId)}: {categoryId}");

            int? top = null;
            if (_blogConfig.FeedSettings.RssItemCount != 0)
            {
                top = _blogConfig.FeedSettings.RssItemCount;
            }

            var query = GetSubscribedPosts(categoryId, top);

            var items = query.Select(p => p.PostPublish.PubDateUtc != null ? new SimpleFeedItem
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                PubDateUtc = p.PostPublish.PubDateUtc.Value,
                Description = p.ContentAbstract,
                Link = GetPostLink(p.PostPublish.PubDateUtc.Value, p.Slug),
                Author = _blogConfig.FeedSettings.AuthorName,
                AuthorEmail = _blogConfig.EmailConfiguration.AdminEmail,
                Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToList()
            } : null);

            return items.ToList();
        }

        private string GetPostLink(DateTime pubDateUtc, string slug)
        {
            return $"{_baseUrl}/post/{pubDateUtc.Year}/{pubDateUtc.Month}/{pubDateUtc.Day}/{slug}";
        }

        private IQueryable<Post> GetSubscribedPosts(Guid? categoryId, int? top = null)
        {
            var query = Context.Post.Where(p => !p.PostPublish.IsDeleted &&
                                         p.PostPublish.IsPublished &&
                                         p.PostPublish.IsFeedIncluded &&
                                         (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)))
                                    .OrderByDescending(p => p.PostPublish.PubDateUtc).AsNoTracking();

            return top.HasValue ? query.Take(top.Value) : query;
        }
    }
}
