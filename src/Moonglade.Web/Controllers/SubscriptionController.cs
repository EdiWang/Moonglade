using LiteBus.Queries.Abstractions;
using Moonglade.Features.Category;
using Moonglade.Syndication;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
public class SubscriptionController(
        IBlogConfig blogConfig,
        ICacheAside cache,
        IQueryMediator queryMediator) : ControllerBase
{
    [HttpGet("opml")]
    public async Task<IActionResult> Opml()
    {
        if (!blogConfig.AdvancedSettings.EnableOpml) return NotFound();

        var cats = await queryMediator.QueryAsync(new ListCategoriesQuery());
        var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.Slug));
        var rootUrl = UrlHelper.ResolveRootUrl(HttpContext, blogConfig.GeneralSettings.CanonicalPrefix);

        var oi = new OpmlDoc
        {
            SiteTitle = $"{blogConfig.GeneralSettings.SiteTitle} - OPML",
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
            s => new GetRssStringQuery(s));
    }

    [HttpGet("atom/{slug?}")]
    public async Task<IActionResult> Atom([MaxLength(64)] string slug = null)
    {
        return await GetFeedAsync(
            slug,
            BlogCachePartition.AtomCategory,
            "atom",
            s => new GetAtomStringQuery(s));
    }

    private async Task<IActionResult> GetFeedAsync<TQuery>(
        string slug,
        BlogCachePartition categoryPartition,
        string defaultCacheKey,
        Func<string, TQuery> queryFactory)
        where TQuery : IQuery<string>
    {
        bool hasRoute = !string.IsNullOrWhiteSpace(slug);
        var route = hasRoute ? slug.ToLower().Trim() : null;

        return await cache.GetOrCreateAsync(
            hasRoute ? categoryPartition.ToString() : BlogCachePartition.General.ToString(),
            route ?? defaultCacheKey,
            async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await queryMediator.QueryAsync(queryFactory(slug));
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (IActionResult)NotFound();
                }

                return Content(xml, "text/xml");
            });
    }
}