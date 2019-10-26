using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Route("category")]
    public partial class CategoryController : MoongladeController
    {
        private readonly PostService _postService;

        private readonly CategoryService _categoryService;

        private readonly IBlogConfig _blogConfig;

        public CategoryController(
            ILogger<CategoryController> logger,
            IOptions<AppSettings> settings,
            CategoryService categoryService,
            PostService postService,
            IBlogConfig blogConfig)
            : base(logger, settings)
        {
            _postService = postService;
            _categoryService = categoryService;

            _blogConfig = blogConfig;
        }

        [Route("list/{categoryName}")]
        public async Task<IActionResult> List(string categoryName, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return NotFound();
            }

            var pageSize = _blogConfig.ContentSettings.PostListPageSize;
            var catResponse = await _categoryService.GetCategoryAsync(categoryName);
            if (!catResponse.IsSuccess)
            {
                return ServerError($"Unsuccessful response: {catResponse.Message}");
            }

            var cat = catResponse.Item;
            if (null == cat)
            {
                Logger.LogWarning($"Category '{categoryName}' not found.");
                return NotFound();
            }

            ViewBag.CategoryDisplayName = cat.DisplayName;
            ViewBag.CategoryName = cat.Name;
            ViewBag.CategoryDescription = cat.Note;

            var postCount = _postService.CountByCategoryId(cat.Id).Item;
            var postList = await _postService.GetPagedPostsAsync(pageSize, page, cat.Id);

            var postsAsIPagedList = new StaticPagedList<PostListItem>(postList, page, pageSize, postCount);
            return View(postsAsIPagedList);
        }
    }
}