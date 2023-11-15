using Moonglade.Core.CategoryFeature;
using Moonglade.Syndication;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
public class SubscriptionController(
        IBlogConfig blogConfig,
        ICacheAside cache,
        IMediator mediator) : ControllerBase
{
    [HttpGet("opml")]
    public async Task<IActionResult> Opml()
    {
        if (!blogConfig.AdvancedSettings.EnableOpml) return NotFound();

        var cats = await mediator.Send(new GetCategoriesQuery());
        var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));
        var rootUrl = Helper.ResolveRootUrl(HttpContext, blogConfig.GeneralSettings.CanonicalPrefix);

        var oi = new OpmlDoc
        {
            SiteTitle = $"{blogConfig.GeneralSettings.SiteTitle} - OPML",
            ContentInfo = catInfos,
            HtmlUrl = $"{rootUrl}/post",
            XmlUrl = $"{rootUrl}/rss",
            XmlUrlTemplate = $"{rootUrl}/rss/[catTitle]",
            HtmlUrlTemplate = $"{rootUrl}/category/[catTitle]"
        };

        var xml = await mediator.Send(new GetOpmlQuery(oi));
        return Content(xml, "text/xml");
    }

    [HttpGet("rss/{routeName?}")]
    public async Task<IActionResult> Rss([MaxLength(64)] string routeName = null)
    {
        bool hasRoute = !string.IsNullOrWhiteSpace(routeName);
        var route = hasRoute ? routeName.ToLower().Trim() : null;

        return await cache.GetOrCreateAsync(
            hasRoute ? BlogCachePartition.RssCategory.ToString() : BlogCachePartition.General.ToString(), route ?? "rss", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await mediator.Send(new GetRssStringQuery(routeName));
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (IActionResult)NotFound();
                }

                return Content(xml, "text/xml");
            });
    }

    [HttpGet("atom/{routeName?}")]
    public async Task<IActionResult> Atom([MaxLength(64)] string routeName = null)
    {
        bool hasRoute = !string.IsNullOrWhiteSpace(routeName);
        var route = hasRoute ? routeName.ToLower().Trim() : null;

        return await cache.GetOrCreateAsync(
            hasRoute ? BlogCachePartition.AtomCategory.ToString() : BlogCachePartition.General.ToString(), route ?? "atom", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);

            var xml = await mediator.Send(new GetAtomStringQuery(routeName));
            return Content(xml, "text/xml");
        });
    }
}