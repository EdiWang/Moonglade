using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Pages;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly IPostQueryService _postQueryService;
        private readonly IPageService _pageService;
        private readonly IBlogCache _cache;
        private readonly IBlogConfig _blogConfig;
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _settings;

        public HomeController(
            IPostQueryService postQueryService,
            IPageService pageService,
            IBlogCache cache,
            IBlogConfig blogConfig,
            ILogger<HomeController> logger,
            IOptions<AppSettings> settingsOptions)
        {
            _postQueryService = postQueryService;
            _pageService = pageService;
            _cache = cache;
            _blogConfig = blogConfig;
            _logger = logger;
            _settings = settingsOptions.Value;
        }

        [Route("page/{slug:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> Page(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

            var page = await _cache.GetOrCreateAsync(CacheDivision.Page, slug.ToLower(), async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Page"]);

                var p = await _pageService.GetAsync(slug);
                return p;
            });

            if (page is null || !page.IsPublished) return NotFound();
            return View(page);
        }

        [Route("category/{routeName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> CategoryList([FromServices] ICategoryService categoryService, string routeName, int p = 1)
        {
            if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

            var pageSize = _blogConfig.ContentSettings.PostListPageSize;
            var cat = await categoryService.Get(routeName);

            if (cat is null) return NotFound();

            ViewBag.CategoryDisplayName = cat.DisplayName;
            ViewBag.CategoryRouteName = cat.RouteName;
            ViewBag.CategoryDescription = cat.Note;

            var postCount = _cache.GetOrCreate(CacheDivision.PostCountCategory, cat.Id.ToString(),
                _ => _postQueryService.CountByCategory(cat.Id));

            var postList = await _postQueryService.List(pageSize, p, cat.Id);

            var postsAsIPagedList = new StaticPagedList<PostDigest>(postList, p, pageSize, postCount);
            return View(postsAsIPagedList);
        }

        [HttpGet("set-lang")]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(culture)) return BadRequest();

                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new(culture)),
                    new() { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );

                return LocalRedirect(returnUrl);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message, culture, returnUrl);
                return LocalRedirect(returnUrl);
            }
        }
    }
}
