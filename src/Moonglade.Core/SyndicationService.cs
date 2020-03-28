using System;
using System.Collections.Generic;
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
        private readonly IBlogConfig _blogConfig;

        private readonly string _baseUrl;

        private readonly IRepository<PostEntity> _postRepository;

        private readonly IHtmlCodec _htmlCodec;

        private readonly ISyndicationData _syndicationData;

        public SyndicationService(
            ILogger<SyndicationService> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            IHttpContextAccessor httpContextAccessor,
            IRepository<PostEntity> postRepository,
            IHtmlCodec htmlCodec, 
            ISyndicationData syndicationData) : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _postRepository = postRepository;
            _htmlCodec = htmlCodec;
            _syndicationData = syndicationData;

            var acc = httpContextAccessor;
            _baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";
        }

        public async Task RefreshRssFilesForCategoryAsync(string categoryName)
        {
            var cid = await _syndicationData.GetCategoryId(categoryName);
            if (cid != Guid.Empty)
            {
                Logger.LogInformation($"Start refreshing RSS feed for category {categoryName}.");

                var itemCollection = await GetPostsAsFeedItemsAsync(cid);

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
