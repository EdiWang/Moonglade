using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;

namespace Moonglade.Web.Controllers
{
    public class SearchController : BlogController
    {
        private readonly SearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(
            SearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        [Route("opensearch")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> OpenSearch([FromServices] IBlogConfig blogConfig)
        {
            var bytes = await _searchService.GetOpenSearchStreamArray(ResolveRootUrl(blogConfig, true));
            var xmlContent = Encoding.UTF8.GetString(bytes);
            return Content(xmlContent, "text/xml");
        }

        [Route("sitemap.xml")]
        public async Task<IActionResult> SiteMap([FromServices] IBlogConfig blogConfig, [FromServices] IBlogCache cache)
        {
            return await cache.GetOrCreateAsync(CacheDivision.General, "sitemap", async entry =>
            {
                var url = ResolveRootUrl(blogConfig);
                var bytes = await _searchService.GetSiteMapStreamArrayAsync(url);
                var xmlContent = Encoding.UTF8.GetString(bytes);
                return Content(xmlContent, "text/xml");
            });
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

            _logger.LogInformation("Searching post for keyword: " + term);

            ViewBag.TitlePrefix = term;

            var posts = await _searchService.SearchAsync(term);
            return View(posts);
        }
    }
}