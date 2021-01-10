using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model.Settings;
using Moonglade.Syndication;

namespace Moonglade.Web.Controllers
{
    public class SubscriptionController : BlogController
    {
        private readonly ISyndicationService _syndicationService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;

        public SubscriptionController(
            ISyndicationService syndicationService,
            CategoryService categoryService,
            IBlogConfig blogConfig)
        {
            _syndicationService = syndicationService;
            _categoryService = categoryService;
            _blogConfig = blogConfig;
        }

        [FeatureGate(FeatureFlags.OPML)]
        [Route("opml")]
        public async Task<IActionResult> Opml([FromServices] IBlogConfig blogConfig, [FromServices] IMemoryStreamOpmlWriter opmlWriter)
        {
            var cats = await _categoryService.GetAllAsync();
            var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));

            var oi = new OpmlDoc
            {
                SiteTitle = $"{_blogConfig.GeneralSettings.SiteTitle} - OPML",
                CategoryInfo = catInfos,
                HtmlUrl = $"{ResolveRootUrl(blogConfig)}/post",
                XmlUrl = $"{ResolveRootUrl(blogConfig)}/rss",
                CategoryXmlUrlTemplate = $"{ResolveRootUrl(blogConfig)}/rss/[catTitle]",
                CategoryHtmlUrlTemplate = $"{ResolveRootUrl(blogConfig)}/category/[catTitle]"
            };

            var bytes = await opmlWriter.GetOpmlStreamDataAsync(oi);
            var xmlContent = Encoding.UTF8.GetString(bytes);
            return Content(xmlContent, "text/xml");
        }

        [Route("rss/{routeName?}")]
        public async Task<IActionResult> Rss([FromServices] IBlogCache cache, string routeName = null)
        {
            bool hasRoute = !string.IsNullOrWhiteSpace(routeName);
            var route = hasRoute ? routeName.ToLower().Trim() : null;

            return await cache.GetOrCreateAsync(
                hasRoute ? CacheDivision.PostCountCategory : CacheDivision.General, route ?? "rss", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await _syndicationService.GetRssDataAsync(routeName);
                if (string.IsNullOrWhiteSpace(xml))
                {
                    return (IActionResult)NotFound();
                }

                return Content(xml, "text/xml");
            });
        }

        [Route("atom")]
        public async Task<IActionResult> Atom([FromServices] IBlogCache cache)
        {
            return await cache.GetOrCreateAsync(CacheDivision.General, "atom", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await _syndicationService.GetAtomData();
                return Content(xml, "text/xml");
            });
        }
    }
}