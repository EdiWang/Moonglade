using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.DTO;
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
    BlogDbContext db,
    IRepositoryBase<PostEntity> postRepo,
    IConfiguration configuration)
    : ISyndicationDataSource
{
    private readonly string _baseUrl = $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";

    public async Task<List<FeedEntry>> GetFeedDataAsync(string catSlug = null)
    {
        List<FeedEntry> itemCollection;
        if (!string.IsNullOrWhiteSpace(catSlug))
        {
            var cat = await db.Category.FirstOrDefaultAsync(c => c.Slug == catSlug);
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
        var dtoSpec = new PostEntityToFeedEntrySpec(_baseUrl);
        var newSpec = postSpec.WithProjectionOf(dtoSpec);

        var list = await postRepo.ListAsync(newSpec);

        // Workaround EF limitation
        // Man, this is super ugly
        if (blogConfig.FeedSettings.UseFullContent && list.Count != 0)
        {
            foreach (var simpleFeedItem in list)
            {
                simpleFeedItem.Description = FormatPostContent(simpleFeedItem.Description, simpleFeedItem.ContentType);
            }
        }

        return list;
    }

    private string FormatPostContent(string rawContent, string contentType)
    {
        var effectiveType = string.IsNullOrEmpty(contentType)
            ? configuration.GetValue<EditorChoice>("Editor").ToString().ToLower()
            : contentType;

        var htmlContent = effectiveType == "markdown" ?
            ContentProcessor.MarkdownToContent(rawContent, ContentProcessor.MarkdownConvertType.Html, false) :
            rawContent;

        if (blogConfig.ImageSettings.EnableCDNRedirect)
        {
            htmlContent = htmlContent.ReplaceCDNEndpointToImgTags(blogConfig.ImageSettings.CDNEndpoint);
        }

        return htmlContent;
    }
}