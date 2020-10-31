using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    public class HomeController : BlogController
    {
        private readonly PostService _postService;
        private readonly IBlogCache _cache;
        private readonly IBlogConfig _blogConfig;

        public HomeController(
            ILogger<ControllerBase> logger, PostService postService, IBlogCache cache, IBlogConfig blogConfig) : base(logger)
        {
            _postService = postService;
            _cache = cache;
            _blogConfig = blogConfig;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                var pagesize = _blogConfig.ContentSettings.PostListPageSize;
                var posts = await _postService.GetPagedPostsAsync(pagesize, page);
                var count = _cache.GetOrCreate(CacheDivision.General, "postcount", entry => _postService.CountVisiblePosts());

                var list = new StaticPagedList<PostListEntry>(posts, page, pagesize, count);
                return View(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error getting post list.");
                return ServerError("Error getting post list.");
            }
        }

        [HttpGet("set-lang")]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
    }
}
