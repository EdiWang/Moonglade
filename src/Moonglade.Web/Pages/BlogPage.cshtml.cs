using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Pages;

namespace Moonglade.Web.Pages
{
    public class BlogPageModel : PageModel
    {
        private readonly IBlogPageService _blogPageService;
        private readonly IBlogCache _cache;
        private readonly AppSettings _settings;
        public BlogPage BlogPage { get; set; }

        public BlogPageModel(
            IBlogPageService blogPageService, IBlogCache cache, IOptions<AppSettings> settingsOptions)
        {
            _blogPageService = blogPageService;
            _cache = cache;
            _settings = settingsOptions.Value;
        }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

            var page = await _cache.GetOrCreateAsync(CacheDivision.Page, slug.ToLower(), async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Page"]);

                var p = await _blogPageService.GetAsync(slug);
                return p;
            });

            if (page is null || !page.IsPublished) return NotFound();

            BlogPage = page;
            return Page();
        }
    }
}
