using LiteBus.Queries.Abstractions;
using Moonglade.Core.CategoryFeature;
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
        bool hasRoute = !string.IsNullOrWhiteSpace(slug);
        var route = hasRoute ? slug.ToLower().Trim() : null;

        return await cache.GetOrCreateAsync(
            hasRoute ? BlogCachePartition.RssCategory.ToString() : BlogCachePartition.General.ToString(), route ?? "rss", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await queryMediator.QueryAsync(new GetRssStringQuery(slug));
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (IActionResult)NotFound();
                }

                return Content(xml, "text/xml");
            });
    }

    [HttpGet("atom/{slug?}")]
    public async Task<IActionResult> Atom([MaxLength(64)] string slug = null)
    {
        bool hasRoute = !string.IsNullOrWhiteSpace(slug);
        var route = hasRoute ? slug.ToLower().Trim() : null;

        return await cache.GetOrCreateAsync(
            hasRoute ? BlogCachePartition.AtomCategory.ToString() : BlogCachePartition.General.ToString(), route ?? "atom", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);

            var xml = await queryMediator.QueryAsync(new GetAtomStringQuery(slug));
            return Content(xml, "text/xml");
        });
    }
}