using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public interface ISyndicationDataSource
{
    Task<IReadOnlyList<FeedEntry>> GetFeedDataAsync(string categoryName = null);
}

public class SyndicationDataSource : ISyndicationDataSource
{
    private readonly string _baseUrl;
    private readonly AppSettings _settings;
    private readonly IBlogConfig _blogConfig;
    private readonly IRepository<CategoryEntity> _catRepo;
    private readonly IRepository<PostEntity> _postRepo;

    public SyndicationDataSource(
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

    public async Task<IReadOnlyList<FeedEntry>> GetFeedDataAsync(string categoryName = null)
    {
        IReadOnlyList<FeedEntry> itemCollection;
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var cat = await _catRepo.GetAsync(c => c.RouteName == categoryName);
            if (cat is null) return null;

            itemCollection = await GetFeedEntriesAsync(cat.Id);
        }
        else
        {
            itemCollection = await GetFeedEntriesAsync();
        }

        return itemCollection;
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
            AuthorEmail = _blogConfig.GeneralSettings.OwnerEmail,
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