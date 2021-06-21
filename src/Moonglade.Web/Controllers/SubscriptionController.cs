using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Syndication;
using Moonglade.Utils;

namespace Moonglade.Web.Controllers
{
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISyndicationService _syndicationService;
        private readonly ICategoryService _catService;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogCache _cache;
        private readonly IOpmlWriter _opmlWriter;

        public SubscriptionController(
            ISyndicationService syndicationService,
            ICategoryService catService,
            IBlogConfig blogConfig,
            IBlogCache cache,
            IOpmlWriter opmlWriter)
        {
            _syndicationService = syndicationService;
            _catService = catService;
            _blogConfig = blogConfig;
            _cache = cache;
            _opmlWriter = opmlWriter;
        }

        [FeatureGate(FeatureFlags.OPML)]
        [HttpGet("opml")]
        public async Task<IActionResult> Opml()
        {
            var cats = await _catService.GetAllAsync();
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

            var xml = await _opmlWriter.GetOpmlDataAsync(oi);
            return Content(xml, "text/xml");
        }

        [HttpGet("rss/{routeName?}")]
        public async Task<IActionResult> Rss(string routeName = null)
        {
            bool hasRoute = !string.IsNullOrWhiteSpace(routeName);
            var route = hasRoute ? routeName.ToLower().Trim() : null;

            return await _cache.GetOrCreateAsync(
                hasRoute ? CacheDivision.PostCountCategory : CacheDivision.General, route ?? "rss", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await _syndicationService.GetRssStringAsync(routeName);
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
            return await _cache.GetOrCreateAsync(CacheDivision.General, "atom", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var xml = await _syndicationService.GetAtomStringAsync();
                return Content(xml, "text/xml");
            });
        }
    }
}