using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Syndication;

namespace Moonglade.Web.Controllers
{
    public class SubscriptionController : BlogController
    {
        private readonly SyndicationService _syndicationService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IMemoryStreamOpmlWriter _fileSystemOpmlWriter;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            SyndicationService syndicationService,
            CategoryService categoryService,
            IBlogConfig blogConfig,
            IMemoryStreamOpmlWriter fileSystemOpmlWriter,
            ILogger<SubscriptionController> logger)
        {
            _syndicationService = syndicationService;
            _categoryService = categoryService;
            _blogConfig = blogConfig;
            _fileSystemOpmlWriter = fileSystemOpmlWriter;
            _logger = logger;
        }

        [Route("/opml")]
        public async Task<IActionResult> Opml([FromServices] IBlogConfig blogConfig)
        {
            var cats = await _categoryService.GetAllAsync();
            var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));

            var oi = new OpmlDoc
            {
                SiteTitle = $"{_blogConfig.GeneralSettings.SiteTitle} - OPML",
                CategoryInfo = catInfos,
                HtmlUrl = $"{ResolveRootUrl(blogConfig)}/post",
                XmlUrl = $"{ResolveRootUrl(blogConfig)}/rss",
                CategoryXmlUrlTemplate = $"{ResolveRootUrl(blogConfig)}/rss/category/[catTitle]",
                CategoryHtmlUrlTemplate = $"{ResolveRootUrl(blogConfig)}/category/list/[catTitle]"
            };

            var bytes = await _fileSystemOpmlWriter.WriteOpmlStreamAsync(oi);
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

                var bytes = await _syndicationService.GetRssStreamDataAsync(routeName);
                var xmlContent = Encoding.UTF8.GetString(bytes);
                return Content(xmlContent, "text/xml");
            });
        }

        [Route("atom")]
        public async Task<IActionResult> Atom([FromServices] IBlogCache cache)
        {
            return await cache.GetOrCreateAsync(CacheDivision.General, "atom", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);

                var bytes = await _syndicationService.GetAtomStreamData();
                var xmlContent = Encoding.UTF8.GetString(bytes);
                return Content(xmlContent, "text/xml");
            });
        }
    }
}