using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Pages;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPostService _postService;
        private readonly IPageService _pageService;
        private readonly ITagService _tagService;
        private readonly IBlogCache _cache;
        private readonly IBlogConfig _blogConfig;
        private readonly ILogger<HomeController> _logger;
        private readonly AppSettings _settings;

        public HomeController(
            IPostService postService,
            IPageService pageService,
            ITagService tagService,
            IBlogCache cache,
            IBlogConfig blogConfig,
            ILogger<HomeController> logger,
            IOptions<AppSettings> settingsOptions)
        {
            _postService = postService;
            _pageService = pageService;
            _tagService = tagService;
            _cache = cache;
            _blogConfig = blogConfig;
            _logger = logger;
            _settings = settingsOptions.Value;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _postService.List(pagesize, page);
            var count = _cache.GetOrCreate(CacheDivision.General, "postcount", _ => _postService.CountVisible());

            var list = new StaticPagedList<PostDigest>(posts, page, pagesize, count);
            return View(list);
        }

        [HttpGet("page/{slug:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
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


        [Route("tags")]
        public async Task<IActionResult> Tags()
        {
            var tags = await _tagService.GetTagCountListAsync();
            return View(tags);
        }

        [Route("tags/{normalizedName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> TagList(string normalizedName, int page = 1)
        {
            var tagResponse = _tagService.Get(normalizedName);
            if (tagResponse is null) return NotFound();

            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _postService.ListByTag(tagResponse.Id, pagesize, page);
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
                _ => _postService.CountByCategory(cat.Id));

            var postList = await _postService.List(pageSize, page, cat.Id);

            var postsAsIPagedList = new StaticPagedList<PostDigest>(postList, page, pageSize, postCount);
            return View(postsAsIPagedList);
        }

        [Route("archive")]
        public async Task<IActionResult> Archive([FromServices] IBlogArchiveService blogArchiveService)
        {
            var archives = await blogArchiveService.ListAsync();
            return View(archives);
        }

        [Route("archive/{year:int:length(4)}")]
        [Route("archive/{year:int:length(4)}/{month:int:range(1,12)}")]
        public async Task<IActionResult> ArchiveList([FromServices] IBlogArchiveService blogArchiveService, int year, int? month)
        {
            if (year > DateTime.UtcNow.Year) return BadRequest();

            IReadOnlyList<PostDigest> model;

            if (month is not null)
            {
                // {year}/{month}
                ViewBag.ArchiveInfo = $"{year}.{month}";
                model = await blogArchiveService.ListPostsAsync(year, month.Value);
            }
            else
            {
                // {year}
                ViewBag.ArchiveInfo = $"{year}";
                model = await blogArchiveService.ListPostsAsync(year);
            }

            return View(model);
        }

        [Route("archive/featured")]
        public async Task<IActionResult> Featured(int page = 1)
        {
            var pagesize = _blogConfig.ContentSettings.PostListPageSize;
            var posts = await _postService.ListFeatured(pagesize, page);
            var count = _cache.GetOrCreate(CacheDivision.PostCountFeatured, "featured", _ => _postService.CountByFeatured());

            var list = new StaticPagedList<PostDigest>(posts, page, pagesize, count);
            return View(list);
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
