using Moonglade.Core.CategoryFeature;
using Moonglade.Syndication;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
public class SubscriptionController : ControllerBase
{
    private readonly IBlogConfig _blogConfig;
    private readonly ICacheAside _cache;
    private readonly IMediator _mediator;

    public SubscriptionController(
        IBlogConfig blogConfig,
        ICacheAside cache,
        IMediator mediator)
    {
        _blogConfig = blogConfig;
        _cache = cache;
        _mediator = mediator;
    }

    [HttpGet("opml")]
    public async Task<IActionResult> Opml()
    {
        if (!_blogConfig.AdvancedSettings.EnableOpml) return NotFound();

        var cats = await _mediator.Send(new GetCategoriesQuery());
        var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));
        var rootUrl = Helper.ResolveRootUrl(HttpContext, _blogConfig.GeneralSettings.CanonicalPrefix);

        var oi = new OpmlDoc
        {
            SiteTitle = $"{_blogConfig.GeneralSettings.SiteTitle} - OPML",
            ContentInfo = catInfos,
            HtmlUrl = $"{rootUrl}/post",
            XmlUrl = $"{rootUrl}/rss",
            XmlUrlTemplate = $"{rootUrl}/rss/[catTitle]",
            HtmlUrlTemplate = $"{rootUrl}/category/[catTitle]"
        };

        var xml = await _mediator.Send(new GetOpmlQuery(oi));
        return Content(xml, "text/xml");
    }

    [HttpGet("rss/{routeName?}")]
    public async Task<IActionResult> Rss([MaxLength(64)] string routeName = null)
    {
        bool hasRoute = !string.IsNullOrWhiteSpace(routeName);
        var route = hasRoute ? routeName.ToLower().Trim() : null;

        return await _cache.GetOrCreateAsync(
            hasRoute ? BlogCachePartition.PostCountCategory.ToString() : BlogCachePartition.General.ToString(), route ?? "rss", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await _mediator.Send(new GetRssStringQuery(routeName));
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (IActionResult)NotFound();
                }

                return Content(xml, "text/xml");
            });
    }

    [HttpGet("atom")]
    public async Task<IActionResult> Atom()
    {
        return await _cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "atom", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);

            var xml = await _mediator.Send(new GetAtomStringQuery());
            return Content(xml, "text/xml");
        });
    }
}