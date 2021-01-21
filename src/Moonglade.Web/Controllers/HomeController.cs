using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    public class HomeController : BlogController
    {
        private readonly IPostService _postService;
        private readonly IBlogCache _cache;
        private readonly IBlogConfig _blogConfig;

        public HomeController(
            IPostService postService,
            IBlogCache cache,
            IBlogConfig blogConfig)
        {
            _postService = postService;
            _cache = cache;
            _blogConfig = blogConfig;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _postService.GetPagedPostsAsync(pagesize, page);
            var count = _cache.GetOrCreate(CacheDivision.General, "postcount", _ => _postService.CountVisiblePosts());

            var list = new StaticPagedList<PostDigest>(posts, page, pagesize, count);
            return View(list);
        }

        [Route("tags")]
        public async Task<IActionResult> Tags([FromServices] ITagService tagService)
        {
            var tags = await tagService.GetTagCountListAsync();
            return View(tags);
        }

        [Route("tags/{normalizedName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> TagList([FromServices] ITagService tagService, string normalizedName, int page = 1)
        {
            var tagResponse = tagService.Get(normalizedName);
            if (tagResponse is null) return NotFound();

            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _postService.GetByTagAsync(tagResponse.Id, pagesize, page);
            var count = _cache.GetOrCreate(CacheDivision.PostCountTag, tagResponse.Id.ToString(), _ => _postService.CountByTag(tagResponse.Id));

            ViewBag.TitlePrefix = tagResponse.DisplayName;

            var list = new StaticPagedList<PostDigest>(posts, page, pagesize, count);
            return View(list);
        }

        [Route("category/{routeName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> CategoryList([FromServices] ICategoryService categoryService, string routeName, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

            var pageSize = _blogConfig.ContentSettings.PostListPageSize;
            var cat = await categoryService.GetAsync(routeName);

            if (cat is null) return NotFound();

            ViewBag.CategoryDisplayName = cat.DisplayName;
            ViewBag.CategoryRouteName = cat.RouteName;
            ViewBag.CategoryDescription = cat.Note;

            var postCount = _cache.GetOrCreate(CacheDivision.PostCountCategory, cat.Id.ToString(),
                _ => _postService.CountByCategoryId(cat.Id));

            var postList = await _postService.GetPagedPostsAsync(pageSize, page, cat.Id);

            var postsAsIPagedList = new StaticPagedList<PostDigest>(postList, page, pageSize, postCount);
            return View(postsAsIPagedList);
        }

        [Route("archive")]
        public async Task<IActionResult> Archive([FromServices] IPostArchiveService postArchiveService)
        {
            var archives = await postArchiveService.ListAsync();
            return View(archives);
        }

        [Route("archive/{year:int:length(4)}")]
        [Route("archive/{year:int:length(4)}/{month:int:range(1,12)}")]
        public async Task<IActionResult> ArchiveList([FromServices] IPostArchiveService postArchiveService, int year, int? month)
        {
            if (year > DateTime.UtcNow.Year) return BadRequest();

            IReadOnlyList<PostDigest> model;

            if (month is not null)
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
            if (string.IsNullOrWhiteSpace(culture)) return BadRequest();

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new(culture)),
                new() { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }
    }
}
