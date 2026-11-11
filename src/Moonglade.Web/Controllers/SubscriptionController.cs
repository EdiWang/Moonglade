using LiteBus.Queries.Abstractions;
using Moonglade.Features.Category;
using Moonglade.Syndication;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
public class SubscriptionController(
        IBlogConfig blogConfig,
        ICacheAside cache,
        PostCacheInvalidator postCacheInvalidator,
        IQueryMediator queryMediator,
        IOwnerProfileReader ownerProfiles) : ControllerBase
{
    [HttpGet("opml")]
    public async Task<IActionResult> Opml()
    {
        var settings = blogConfig.Capture();
        if (!settings.AdvancedSettings.EnableOpml) return NotFound();

        var cats = await queryMediator.QueryAsync(new ListCategoriesQuery());
        var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.Slug));
        var rootUrl = UrlHelper.ResolveRootUrl(HttpContext, settings.SEOSettings.CanonicalPrefix);

        var oi = new OpmlDoc
        {
            SiteTitle = $"{settings.GeneralSettings.SiteTitle} - OPML",
            ContentInfo = catInfos,
            HtmlUrl = $"{rootUrl}/post",
            XmlUrl = $"{rootUrl}/rss",
            XmlUrlTemplate = $"{rootUrl}/rss/[catTitle]",
            HtmlUrlTemplate = $"{rootUrl}/category/[catTitle]"
        };

        var xml = await queryMediator.QueryAsync(new GetOpmlQuery(oi));
        return Content(xml, "text/xml");
    }

    [HttpGet("rss/{slug?}")]
    public async Task<IActionResult> Rss([MaxLength(64)] string slug = null)
    {
        return await GetFeedAsync(
            slug,
            BlogCachePartition.RssCategory,
            "rss",
            (s, settings, _, presentation) => new GetRssStringQuery(s, settings, presentation));
    }

    [HttpGet("atom/{slug?}")]
    public async Task<IActionResult> Atom([MaxLength(64)] string slug = null)
    {
        return await GetFeedAsync(
            slug,
            BlogCachePartition.AtomCategory,
            "atom",
            (s, settings, owner, presentation) => new GetAtomStringQuery(s, settings, owner, presentation));
    }

    private async Task<IActionResult> GetFeedAsync<TQuery>(
        string slug,
        BlogCachePartition categoryPartition,
        string defaultCacheKey,
        Func<string, BlogSettingsSnapshot, OwnerProfile, OwnerPresentation, TQuery> queryFactory)
        where TQuery : IQuery<string>
    {
        var settings = blogConfig.Capture();
        var owner = defaultCacheKey == "atom"
            ? await ownerProfiles.ReadProfileAsync(HttpContext.RequestAborted) : null;
        var presentation = await ownerProfiles.ReadPresentationAsync(HttpContext.RequestAborted);
        bool hasRoute = !string.IsNullOrWhiteSpace(slug);
        var route = hasRoute ? slug.ToLower().Trim() : null;

        return await cache.GetOrCreateAsync(
            hasRoute ? categoryPartition.ToString() : BlogCachePartition.General.ToString(),
            postCacheInvalidator.GetFeedCacheKey(route ?? defaultCacheKey, settings, owner, presentation.Description),
            async () =>
            {
                var xml = await queryMediator.QueryAsync(queryFactory(slug, settings, owner, presentation));
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (IActionResult)NotFound();
                }

                return Content(xml, "text/xml");
            }, TimeSpan.FromHours(1));
    }
}
