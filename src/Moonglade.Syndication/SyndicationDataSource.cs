using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public class SyndicationDataSource(
    IBlogConfig blogConfig,
    IHttpContextAccessor httpContextAccessor,
    BlogDbContext db)
{
    private readonly string _baseUrl = $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";

    public async Task<List<FeedEntry>> GetFeedDataAsync(string catSlug = null, BlogSettingsSnapshot snapshot = null, CancellationToken ct = default)
    {
        var settings = snapshot ?? blogConfig.Capture();
        List<FeedEntry> itemCollection;
        if (!string.IsNullOrWhiteSpace(catSlug))
        {
            var cat = await db.Category.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == catSlug, ct);
            if (cat is null) return null;

            itemCollection = await GetFeedEntriesAsync(settings, cat.Id, ct);
        }
        else
        {
            itemCollection = await GetFeedEntriesAsync(settings, null, ct);
        }

        return itemCollection;
    }

    private async Task<List<FeedEntry>> GetFeedEntriesAsync(BlogSettingsSnapshot settings, int? catId, CancellationToken ct)
    {
        int? top = null;
        if (settings.FeedSettings.FeedItemCount != 0)
        {
            top = settings.FeedSettings.FeedItemCount;
        }

        IQueryable<PostEntity> query = db.Post.AsNoTracking()
            .Where(p => !p.IsDeleted &&
                        p.PostStatus == PostStatus.Published &&
                        p.IsFeedIncluded &&
                        p.PubDateUtc != null &&
                        (catId == null || p.PostCategory.Any(c => c.CategoryId == catId.Value)))
            .OrderByDescending(p => p.PubDateUtc);

        if (top.HasValue)
        {
            query = query.Take(top.Value);
        }

        var useFullContent = settings.FeedSettings.UseFullContent;
        var list = await query.Select(p => new FeedEntry
        {
            Id = p.Id.ToString(),
            Title = p.Title,
            PubDateUtc = p.PubDateUtc.Value,
            Description = useFullContent ? p.PostContent : p.ContentAbstract,
            Link = $"{_baseUrl}/post/{p.RouteLink}",
            Author = p.Author,
            LangCode = p.ContentLanguageCode,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToArray(),
            ContentType = p.ContentType
        }).ToListAsync(ct);

        // Workaround EF limitation
        // Man, this is super ugly
        if (useFullContent && list.Count != 0)
        {
            foreach (var simpleFeedItem in list)
            {
                simpleFeedItem.Description = FormatPostContent(simpleFeedItem.Description, simpleFeedItem.ContentType, settings.ImageSettings);
            }
        }

        return list;
    }

    private static string FormatPostContent(string rawContent, string contentType, ImageSettings settings)
    {
        var effectiveType = string.IsNullOrEmpty(contentType) ? "html" : contentType;

        var htmlContent = effectiveType == "markdown" ?
            ContentProcessor.MarkdownToContent(rawContent, ContentProcessor.MarkdownConvertType.Html, false) :
            rawContent;

        if (settings.EnableCDNRedirect)
        {
            htmlContent = htmlContent.ReplaceCDNEndpointToImgTags(settings.CDNEndpoint);
        }

        return htmlContent;
    }
}
