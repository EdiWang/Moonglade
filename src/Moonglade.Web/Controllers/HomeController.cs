using System;
using System.Collections.Generic;
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

        [Route("tags")]
        public async Task<IActionResult> Tags([FromServices] TagService tagService)
        {
            try
            {
                var tags = await tagService.GetTagCountListAsync();
                return View(tags);
            }
            catch (Exception e)
            {
                SetFriendlyErrorMessage();
                Logger.LogError(e, e.Message);
                return View();
            }
        }

        [Route("tags/list/{normalizedName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> TagList(string normalizedName, [FromServices] TagService tagService)
        {
            try
            {
                var tagResponse = tagService.Get(normalizedName);
                if (tagResponse == null) return NotFound();

                ViewBag.TitlePrefix = tagResponse.DisplayName;
                var posts = await _postService.GetByTagAsync(tagResponse.Id);

                return View(posts);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                SetFriendlyErrorMessage();
                return View();
            }
        }

        [Route("category/list/{routeName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> CategoryList([FromServices] CategoryService categoryService, string routeName, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

            var pageSize = _blogConfig.ContentSettings.PostListPageSize;
            var cat = await categoryService.GetAsync(routeName);

            if (null == cat)
            {
                Logger.LogWarning($"Category '{routeName}' not found.");
                return NotFound();
            }

            ViewBag.CategoryDisplayName = cat.DisplayName;
            ViewBag.CategoryRouteName = cat.RouteName;
            ViewBag.CategoryDescription = cat.Note;

            var postCount = _cache.GetOrCreate(CacheDivision.PostCountCategory, cat.Id.ToString(),
                entry => _postService.CountByCategoryId(cat.Id));

            var postList = await _postService.GetPagedPostsAsync(pageSize, page, cat.Id);

            var postsAsIPagedList = new StaticPagedList<PostListEntry>(postList, page, pageSize, postCount);
            return View(postsAsIPagedList);
        }

        [Route("archive")]
        public async Task<IActionResult> Archive([FromServices] PostArchiveService postArchiveService)
        {
            try
            {
                var archives = await postArchiveService.ListAsync();
                return View(archives);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                SetFriendlyErrorMessage();
                return View();
            }
        }

        [Route("archive/{year:int:length(4)}")]
        [Route("archive/{year:int:length(4)}/{month:int:range(1,12)}")]
        public async Task<IActionResult> ArchiveList([FromServices] PostArchiveService postArchiveService, int year, int? month)
        {
            if (year > DateTime.UtcNow.Year) return BadRequest();

            IReadOnlyList<PostListEntry> model;

            if (null != month)
            {
                // {year}/{month}
                ViewBag.ArchiveInfo = $"{year}.{month}";
                model = await postArchiveService.ListPostsAsync(year, month.Value);
            }
            else
            {
                // {year}
                ViewBag.ArchiveInfo = $"{year}";
                model = await postArchiveService.ListPostsAsync(year);
            }

            return View(model);
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
