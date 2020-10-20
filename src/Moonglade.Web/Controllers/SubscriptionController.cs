using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Syndication;

namespace Moonglade.Web.Controllers
{
    public class SubscriptionController : BlogController
    {
        private readonly SyndicationService _syndicationFeedService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IFileSystemOpmlWriter _fileSystemOpmlWriter;

        public SubscriptionController(
            ILogger<SubscriptionController> logger,
            IOptions<AppSettings> settings,
            SyndicationService syndicationFeedService,
            CategoryService categoryService,
            IBlogConfig blogConfig,
            IFileSystemOpmlWriter fileSystemOpmlWriter)
            : base(logger, settings)
        {
            _syndicationFeedService = syndicationFeedService;
            _categoryService = categoryService;
            _blogConfig = blogConfig;
            _fileSystemOpmlWriter = fileSystemOpmlWriter;
        }

        [Route("/opml")]
        public async Task<IActionResult> Opml()
        {
            var feedDirectoryPath = Path.Join($"{DataDirectory}", "feed");
            if (!Directory.Exists(feedDirectoryPath))
            {
                Directory.CreateDirectory(feedDirectoryPath);
                Logger.LogInformation($"Created directory '{feedDirectoryPath}'");
            }

            var opmlDataFile = Path.Join($"{DataDirectory}", $"{Constants.OpmlFileName}");
            if (!System.IO.File.Exists(opmlDataFile))
            {
                Logger.LogInformation($"OPML file not found, writing new file on {opmlDataFile}");

                var cats = await _categoryService.GetAllAsync();
                var catInfos = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));

                var oi = new OpmlDoc
                {
                    SiteTitle = $"{_blogConfig.GeneralSettings.SiteTitle} - OPML",
                    CategoryInfo = catInfos,
                    HtmlUrl = $"{SiteRootUrl}/post",
                    XmlUrl = $"{SiteRootUrl}/rss",
                    CategoryXmlUrlTemplate = $"{SiteRootUrl}/rss/category/[catTitle]",
                    CategoryHtmlUrlTemplate = $"{SiteRootUrl}/category/list/[catTitle]"
                };

                var path = Path.Join($"{DataDirectory}", $"{Constants.OpmlFileName}");
                await _fileSystemOpmlWriter.WriteOpmlFileAsync(path, oi);
                Logger.LogInformation("OPML file write completed.");

                if (!System.IO.File.Exists(opmlDataFile))
                {
                    Logger.LogInformation("OPML file still not found, something just went very very wrong...");
                    return NotFound();
                }
            }

            if (System.IO.File.Exists(opmlDataFile))
            {
                return PhysicalFile(opmlDataFile, "text/xml");
            }

            return NotFound();
        }

        [Route("rss/{routeName?}")]
        public async Task<IActionResult> Rss(string routeName = null)
        {
            var rssDataFile = string.IsNullOrWhiteSpace(routeName) ?
                Path.Join($"{DataDirectory}", "feed", "posts.xml") :
                Path.Join($"{DataDirectory}", "feed", $"posts-category-{routeName}.xml");

            if (!System.IO.File.Exists(rssDataFile))
            {
                Logger.LogInformation($"RSS file not found, writing new file on {rssDataFile}");

                if (string.IsNullOrWhiteSpace(routeName))
                {
                    await _syndicationFeedService.RefreshFeedFileAsync(false);
                }
                else
                {
                    await _syndicationFeedService.RefreshRssFilesAsync(routeName.ToLower());
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
            var atomDataFile = Path.Join($"{DataDirectory}", "feed", "posts-atom.xml");
            if (!System.IO.File.Exists(atomDataFile))
            {
                Logger.LogInformation($"Atom file not found, writing new file on {atomDataFile}");

                await _syndicationFeedService.RefreshFeedFileAsync(true);

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