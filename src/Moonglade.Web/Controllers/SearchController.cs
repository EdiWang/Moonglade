using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Utils;

namespace Moonglade.Web.Controllers
{
    public class SearchController : BlogController
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [FeatureGate(FeatureFlags.RSD)]
        [Route("rsd")]
        [ResponseCache(Duration = 7200)]
        public async Task<IActionResult> RSD([FromServices] IRSDService rsdService, [FromServices] IBlogConfig blogConfig)
        {
            var bytes = await rsdService.GetRSDStreamArray(Helper.ResolveRootUrl(HttpContext, blogConfig.GeneralSettings.CanonicalPrefix, true));
            var xmlContent = Encoding.UTF8.GetString(bytes);

            return Content(xmlContent, "text/xml");
        }

        [FeatureGate(FeatureFlags.OpenSearch)]
        [Route("opensearch")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> OpenSearch([FromServices] IBlogConfig blogConfig)
        {
            var bytes = await _searchService.GetOpenSearchStreamArray(Helper.ResolveRootUrl(HttpContext, blogConfig.GeneralSettings.CanonicalPrefix, true));
            var xmlContent = Encoding.UTF8.GetString(bytes);

            return Content(xmlContent, "text/xml");
        }

        [Route("sitemap.xml")]
        public async Task<IActionResult> SiteMap([FromServices] IBlogConfig blogConfig, [FromServices] IBlogCache cache)
        {
            return await cache.GetOrCreateAsync(CacheDivision.General, "sitemap", async _ =>
            {
                var url = Helper.ResolveRootUrl(HttpContext, blogConfig.GeneralSettings.CanonicalPrefix, true);
                var bytes = await _searchService.GetSiteMapStreamArrayAsync(url);
                var xmlContent = Encoding.UTF8.GetString(bytes);

                return Content(xmlContent, "text/xml");
            });
        }

        [HttpPost("search")]
        public IActionResult Post(string term)
        {
            return !string.IsNullOrWhiteSpace(term) ?
                RedirectToAction(nameof(Search), new { term }) :
                RedirectToAction("Index", "Home");
        }

        [HttpGet("search/{term}")]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TitlePrefix = term;

            var posts = await _searchService.SearchAsync(term);
            return View(posts);
        }
    }
}