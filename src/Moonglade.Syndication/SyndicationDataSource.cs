using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public interface ISyndicationDataSource
{
    Task<List<FeedEntry>> GetFeedDataAsync(string catSlug = null);
}

public class SyndicationDataSource(
    IBlogConfig blogConfig,
    IHttpContextAccessor httpContextAccessor,
    MoongladeRepository<CategoryEntity> catRepo,
    MoongladeRepository<PostEntity> postRepo,
    IConfiguration configuration)
    : ISyndicationDataSource
{
    private readonly string _baseUrl = $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";

    public async Task<List<FeedEntry>> GetFeedDataAsync(string catSlug = null)
    {
        List<FeedEntry> itemCollection;
        if (!string.IsNullOrWhiteSpace(catSlug))
        {
            var cat = await catRepo.FirstOrDefaultAsync(new CategoryBySlugSpec(catSlug));
            if (cat is null) return null;

            itemCollection = await GetFeedEntriesAsync(cat.Id);
        }
        else
        {
            itemCollection = await GetFeedEntriesAsync();
        }

        return itemCollection;
    }

    private async Task<List<FeedEntry>> GetFeedEntriesAsync(Guid? catId = null)
    {
        int? top = null;
        if (blogConfig.FeedSettings.FeedItemCount != 0)
        {
            top = blogConfig.FeedSettings.FeedItemCount;
        }

        var postSpec = new PostByCatSpec(catId, top);
        var list = await postRepo.SelectAsync(postSpec, p => p.PubDateUtc != null ? new FeedEntry
        {
            Id = p.Id.ToString(),
            Title = p.Title,
            PubDateUtc = p.PubDateUtc.Value,
            Description = blogConfig.FeedSettings.UseFullContent ? p.PostContent : p.ContentAbstract,
            Link = $"{_baseUrl}/post/{p.PubDateUtc.Value.Year}/{p.PubDateUtc.Value.Month}/{p.PubDateUtc.Value.Day}/{p.Slug}",
            Author = blogConfig.GeneralSettings.OwnerName,
            AuthorEmail = blogConfig.GeneralSettings.OwnerEmail,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToArray()
        } : null);

        // Workaround EF limitation
        // Man, this is super ugly
        if (blogConfig.FeedSettings.UseFullContent && list.Any())
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
        return configuration.GetSection("Editor").Get<EditorChoice>() == EditorChoice.Markdown ?
            ContentProcessor.MarkdownToContent(rawContent, ContentProcessor.MarkdownConvertType.Html, false) :
            rawContent;
    }
}