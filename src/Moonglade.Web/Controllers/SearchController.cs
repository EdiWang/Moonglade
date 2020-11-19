using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;

namespace Moonglade.Web.Controllers
{
    public class SearchController : BlogController
    {
        private readonly SearchService _searchService;

        public SearchController(
            ILogger<SearchController> logger,
            SearchService searchService)
            : base(logger)
        {
            _searchService = searchService;
        }

        [Route("opensearch")]
        public async Task<IActionResult> OpenSearch()
        {
            var bytes = await _searchService.GetOpenSearchStreamArray(RootUrl);
            var xmlContent = Encoding.UTF8.GetString(bytes);
            return Content(xmlContent, "text/xml");
        }

        [Route("sitemap.xml")]
        public async Task<IActionResult> SiteMap([FromServices] IBlogConfig blogConfig)
        {
            var siteMapFile = Path.Join(DataDirectory, Constants.SiteMapFileName);
            if (!System.IO.File.Exists(siteMapFile))
            {
                Logger.LogInformation($"SiteMap file not found, writing new file on {siteMapFile}");

                var url = RootUrl;
                var canonicalUrl = blogConfig.GeneralSettings.CanonicalPrefix;
                if (!string.IsNullOrWhiteSpace(canonicalUrl))
                {
                    url = canonicalUrl;
                }

                await _searchService.WriteSiteMapFileAsync(url, DataDirectory);

                if (!System.IO.File.Exists(siteMapFile))
                {
                    Logger.LogError("SiteMap file still not found, what the heck?!");
                    return NotFound();
                }
            }

            if (System.IO.File.Exists(siteMapFile))
            {
                return PhysicalFile(siteMapFile, "text/xml");
            }

            return NotFound();
        }

        [HttpPost("search")]
        public IActionResult Index(string term)
        {
            return !string.IsNullOrWhiteSpace(term) ?
                RedirectToAction(nameof(SearchGet), new { term }) :
                RedirectToAction("Index", "Home");
        }

        [HttpGet("search/{term}")]
        public async Task<IActionResult> SearchGet(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return RedirectToAction("Index", "Home");
            }

            Logger.LogInformation("Searching post for keyword: " + term);

            ViewBag.TitlePrefix = term;

            var posts = await _searchService.SearchAsync(term);
            return View(posts);
        }
    }
}