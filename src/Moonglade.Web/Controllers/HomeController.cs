using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Pages;

namespace Moonglade.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        private readonly IPageService _pageService;
        private readonly IBlogCache _cache;
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _settings;

        public HomeController(
            IPageService pageService,
            IBlogCache cache,
            ILogger<HomeController> logger,
            IOptions<AppSettings> settingsOptions)
        {
            _pageService = pageService;
            _cache = cache;
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
