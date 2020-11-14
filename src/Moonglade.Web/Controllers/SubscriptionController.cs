using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Syndication;

namespace Moonglade.Web.Controllers
{
    public class SubscriptionController : BlogController
    {
        private readonly SyndicationService _syndicationService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IFileSystemOpmlWriter _fileSystemOpmlWriter;

        public SubscriptionController(
            ILogger<SubscriptionController> logger,
            SyndicationService syndicationService,
            CategoryService categoryService,
            IBlogConfig blogConfig,
            IFileSystemOpmlWriter fileSystemOpmlWriter)
            : base(logger)
        {
            _syndicationService = syndicationService;
            _categoryService = categoryService;
            _blogConfig = blogConfig;
            _fileSystemOpmlWriter = fileSystemOpmlWriter;
        }

        [Route("/opml")]
        public async Task<IActionResult> Opml()
        {
            var feedPath = Path.Join(DataDirectory, "feed");
            if (!Directory.Exists(feedPath))
            {
                Directory.CreateDirectory(feedPath);
                Logger.LogInformation($"Created directory '{feedPath}'");
            }

            var opmlFile = Path.Join(DataDirectory, Constants.OpmlFileName);
            if (!System.IO.File.Exists(opmlFile))
            {
                Logger.LogInformation($"OPML file not found, writing new file on {opmlFile}");

                var cats = await _categoryService.GetAllAsync();
                var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));

                var oi = new OpmlDoc
                {
                    SiteTitle = $"{_blogConfig.GeneralSettings.SiteTitle} - OPML",
                    CategoryInfo = catInfos,
                    HtmlUrl = $"{RootUrl}/post",
                    XmlUrl = $"{RootUrl}/rss",
                    CategoryXmlUrlTemplate = $"{RootUrl}/rss/category/[catTitle]",
                    CategoryHtmlUrlTemplate = $"{RootUrl}/category/list/[catTitle]"
                };

                var path = Path.Join(DataDirectory, Constants.OpmlFileName);
                await _fileSystemOpmlWriter.WriteOpmlFileAsync(path, oi);
                Logger.LogInformation("OPML file write completed.");

                if (!System.IO.File.Exists(opmlFile))
                {
                    Logger.LogInformation("OPML file still not found, something just went very very wrong...");
                    return NotFound();
                }
            }

            if (System.IO.File.Exists(opmlFile))
            {
                return PhysicalFile(opmlFile, "text/xml");
            }

            return NotFound();
        }

        [Route("rss/{routeName?}")]
        public async Task<IActionResult> Rss(string routeName = null)
        {
            var rssDataFile = string.IsNullOrWhiteSpace(routeName) ?
                Path.Join(DataDirectory, "feed", "posts.xml") :
                Path.Join(DataDirectory, "feed", $"posts-category-{routeName}.xml");

            if (!System.IO.File.Exists(rssDataFile))
            {
                Logger.LogInformation($"RSS file not found, writing new file on {rssDataFile}");

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
                Logger.LogInformation($"Atom file not found, writing new file on {atomDataFile}");

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