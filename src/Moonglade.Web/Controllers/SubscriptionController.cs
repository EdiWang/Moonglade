using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        public async Task<IActionResult> Rss(string routeName = null)
        {
            var rssDataFile = string.IsNullOrWhiteSpace(routeName) ?
                Path.Join(DataDirectory, "feed", "posts.xml") :
                Path.Join(DataDirectory, "feed", $"posts-category-{routeName}.xml");

            if (!System.IO.File.Exists(rssDataFile))
            {
                _logger.LogInformation($"RSS file not found, writing new file on {rssDataFile}");

                if (string.IsNullOrWhiteSpace(routeName))
                {
                    await _syndicationService.RefreshFeedFileAsync(false);
                }
                else
                {
                    await _syndicationService.RefreshRssFilesAsync(routeName.ToLower());
                }

                if (!System.IO.File.Exists(rssDataFile))
                {
                    return NotFound();
                }
            }

            if (System.IO.File.Exists(rssDataFile))
            {
                return PhysicalFile(rssDataFile, "text/xml");
            }

            return NotFound();
        }

        [Route("atom")]
        public async Task<IActionResult> Atom()
        {
            var atomDataFile = Path.Join(DataDirectory, "feed", "posts-atom.xml");
            if (!System.IO.File.Exists(atomDataFile))
            {
                _logger.LogInformation($"Atom file not found, writing new file on {atomDataFile}");

                await _syndicationService.RefreshFeedFileAsync(true);

                if (!System.IO.File.Exists(atomDataFile)) return NotFound();
            }

            if (System.IO.File.Exists(atomDataFile))
            {
                return PhysicalFile(atomDataFile, "text/xml");
            }

            return NotFound();
        }
    }
}