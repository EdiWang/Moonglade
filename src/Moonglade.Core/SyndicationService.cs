using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model.Settings;
using Moonglade.Syndication;

namespace Moonglade.Core
{
    public class SyndicationService : BlogService
    {
        private readonly string _baseUrl;
        private readonly AppSettings _settings;
        private readonly IBlogConfig _blogConfig;
        private readonly IRepository<CategoryEntity> _catRepo;
        private readonly IRepository<PostEntity> _postRepo;

        public SyndicationService(
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            IHttpContextAccessor httpContextAccessor,
            IRepository<CategoryEntity> catRepo,
            IRepository<PostEntity> postRepo)
        {
            _settings = settings.Value;
            _blogConfig = blogConfig;
            _catRepo = catRepo;
            _postRepo = postRepo;

            var acc = httpContextAccessor;
            _baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";
        }

        public async Task<byte[]> GetRssStreamDataAsync(string categoryName = null)
        {
            IReadOnlyList<FeedEntry> itemCollection = null;
            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var cat = await _catRepo.GetAsync(c => c.RouteName == categoryName);
                if (cat is null)
                {
                    throw new InvalidDataException($"'{categoryName}' is not found.");
                }

                itemCollection = await GetFeedEntriesAsync(cat.Id);
            }
            else
            {
                itemCollection = await GetFeedEntriesAsync();
            }

            var rw = new FeedGenerator
            {
                HostUrl = _baseUrl,
                HeadTitle = _blogConfig.FeedSettings.RssTitle,
                HeadDescription = _blogConfig.FeedSettings.RssDescription,
                Copyright = _blogConfig.FeedSettings.RssCopyright,
                Generator = $"Moonglade v{Utils.AppVersion}",
                FeedItemCollection = itemCollection,
                TrackBackUrl = _baseUrl,
                MaxContentLength = 0
            };

            using var ms = new MemoryStream();
            await rw.WriteRssStreamAsync(ms);
            await ms.FlushAsync();

            return ms.ToArray();
        }

        public async Task<byte[]> GetAtomStreamData()
        {
            var itemCollection = await GetFeedEntriesAsync();

            var rw = new FeedGenerator
            {
                HostUrl = _baseUrl,
                HeadTitle = _blogConfig.FeedSettings.RssTitle,
                HeadDescription = _blogConfig.FeedSettings.RssDescription,
                Copyright = _blogConfig.FeedSettings.RssCopyright,
                Generator = $"Moonglade v{Utils.AppVersion}",
                FeedItemCollection = itemCollection,
                TrackBackUrl = _baseUrl,
                MaxContentLength = 0
            };

            using var ms = new MemoryStream();
            await rw.WriteAtomStreamAsync(ms);
            await ms.FlushAsync();

            return ms.ToArray();
        }

        private async Task<IReadOnlyList<FeedEntry>> GetFeedEntriesAsync(Guid? categoryId = null)
        {
            int? top = null;
            if (_blogConfig.FeedSettings.RssItemCount != 0)
            {
                top = _blogConfig.FeedSettings.RssItemCount;
            }

            var postSpec = new PostSpec(categoryId, top);
            var list = await _postRepo.SelectAsync(postSpec, p => p.PubDateUtc != null ? new FeedEntry
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                PubDateUtc = p.PubDateUtc.Value,
                Description = _blogConfig.FeedSettings.UseFullContent ? p.PostContent : p.ContentAbstract,
                Link = $"{_baseUrl}/post/{p.PubDateUtc.Value.Year}/{p.PubDateUtc.Value.Month}/{p.PubDateUtc.Value.Day}/{p.Slug}",
                Author = _blogConfig.FeedSettings.AuthorName,
                AuthorEmail = _blogConfig.NotificationSettings.AdminEmail,
                Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToArray()
            } : null);

            // Workaround EF limitation
            // Man, this is super ugly
            if (_blogConfig.FeedSettings.UseFullContent && list.Any())
            {
                foreach (var simpleFeedItem in list)
                {
                    simpleFeedItem.Description = FormatPostContent(simpleFeedItem.Description);
                }
            }

            return list;
        }

        private string FormatPostContent(string rawContent)
        {
            return _settings.Editor == EditorChoice.Markdown ?
                ContentProcessor.MarkdownToContent(rawContent, ContentProcessor.MarkdownConvertType.Html, false) :
                rawContent;
        }
    }
}
