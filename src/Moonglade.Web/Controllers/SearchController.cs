using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Utils;
using Moonglade.Web.BlogProtocols;

namespace Moonglade.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;
        private readonly IBlogConfig _blogConfig;

        public SearchController(ISearchService searchService, IBlogConfig blogConfig)
        {
            _searchService = searchService;
            _blogConfig = blogConfig;
        }

        [FeatureGate(FeatureFlags.RSD)]
        [Route("rsd")]
        [ResponseCache(Duration = 7200)]
        public async Task<IActionResult> RSD()
        {
            var bytes = await RSDWriter.GetRSDStreamArray(Helper.ResolveRootUrl(HttpContext, _blogConfig.GeneralSettings.CanonicalPrefix, true));
            var xmlContent = Encoding.UTF8.GetString(bytes);

            return Content(xmlContent, "text/xml");
        }

        [FeatureGate(FeatureFlags.OpenSearch)]
        [Route("opensearch")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> OpenSearch()
        {
            var bytes = await OpenSearchWriter.GetOpenSearchStreamArray(
                Helper.ResolveRootUrl(HttpContext, _blogConfig.GeneralSettings.CanonicalPrefix, true),
                _blogConfig.GeneralSettings.SiteTitle,
                _blogConfig.GeneralSettings.Description);

            var xmlContent = Encoding.UTF8.GetString(bytes);

            return Content(xmlContent, "text/xml");
        }

        [Route("sitemap.xml")]
        public async Task<IActionResult> SiteMap([FromServices] IBlogCache cache, [FromServices] ISiteMapWriter siteMapWriter)
        {
            return await cache.GetOrCreateAsync(CacheDivision.General, "sitemap", async _ =>
            {
                var url = Helper.ResolveRootUrl(HttpContext, _blogConfig.GeneralSettings.CanonicalPrefix, true);
                var bytes = await siteMapWriter.GetSiteMapStreamArrayAsync(url);
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