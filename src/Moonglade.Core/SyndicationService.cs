using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.HtmlEncoding;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Syndication;

namespace Moonglade.Core
{
    public class SyndicationService : MoongladeService
    {
        private readonly string _baseUrl;

        private readonly IBlogConfig _blogConfig;
        private readonly IRepository<CategoryEntity> _categoryRepository;
        private readonly IRepository<PostEntity> _postRepository;
        private readonly IHtmlCodec _htmlCodec;

        public SyndicationService(
            ILogger<SyndicationService> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            IHttpContextAccessor httpContextAccessor,
            IRepository<CategoryEntity> categoryRepository,
            IRepository<PostEntity> postRepository,
            IHtmlCodec htmlCodec) : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _categoryRepository = categoryRepository;
            _postRepository = postRepository;
            _htmlCodec = htmlCodec;

            var acc = httpContextAccessor;
            _baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";
        }

        public async Task RefreshRssFilesForCategoryAsync(string categoryName)
        {
            var cat = await _categoryRepository.GetAsync(c => c.Title == categoryName);
            if (null != cat)
            {
                Logger.LogInformation($"Start refreshing RSS feed for category {categoryName}.");

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

                var path = Path.Join($"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}", "feed", $"posts-category-{categoryName}.xml");

                await rw.WriteRss20FileAsync(path);
                Logger.LogInformation($"Finished refreshing RSS feed for category {categoryName}.");
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

                var path = Path.Join($"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}", "feed", "posts-atom.xml");
                await rw.WriteAtom10FileAsync(path);
            }
            else
            {
                Logger.LogInformation("Writing RSS file.");

                var path = Path.Join($"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}", "feed",
                    "posts.xml");
                await rw.WriteRss20FileAsync(path);
            }

            Logger.LogInformation("Finished writing feed for posts.");
        }

        private async Task<IReadOnlyList<SimpleFeedItem>> GetPostsAsFeedItemsAsync(Guid? categoryId = null)
        {
            int? top = null;
            if (_blogConfig.FeedSettings.RssItemCount != 0)
            {
                top = _blogConfig.FeedSettings.RssItemCount;
            }

            var postSpec = new PostSpec(categoryId, top);
            var list = await _postRepository.SelectAsync(postSpec, p => p.PostPublish.PubDateUtc != null ? new SimpleFeedItem
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                PubDateUtc = p.PostPublish.PubDateUtc.Value,
                Description = _blogConfig.FeedSettings.UseFullContent ? p.PostContent : p.ContentAbstract,
                Link = $"{_baseUrl}/post/{p.PostPublish.PubDateUtc.Value.Year}/{p.PostPublish.PubDateUtc.Value.Month}/{p.PostPublish.PubDateUtc.Value.Day}/{p.Slug}",
                Author = _blogConfig.FeedSettings.AuthorName,
                AuthorEmail = _blogConfig.EmailSettings.AdminEmail,
                Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToList()
            } : null);

            // Workaround EF limitation
            // Man, this is super ugly
            if (_blogConfig.FeedSettings.UseFullContent && list.Any())
            {
                foreach (var simpleFeedItem in list)
                {
                    simpleFeedItem.Description = GetPostContent(simpleFeedItem.Description);
                }
            }

            return list;
        }

        private string GetPostContent(string rawContent)
        {
            var editor = AppSettings.Editor;
            switch (editor)
            {
                case EditorChoice.HTML:
                    var html = _htmlCodec.HtmlDecode(rawContent);
                    return html;
                case EditorChoice.Markdown:
                    var md2Html = Utils.ConvertMarkdownContent(rawContent, Utils.MarkdownConvertType.Html, false);
                    return md2Html;
                case EditorChoice.None:
                default:
                    return rawContent;
            }
        }
    }
}
