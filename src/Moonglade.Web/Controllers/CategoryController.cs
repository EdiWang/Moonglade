using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data.Entities;
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

        public CategoryController(
            ILogger<CategoryController> logger,
            IOptions<AppSettings> settings,
            CategoryService categoryService,
            PostService postService)
            : base(logger, settings)
        {
            _postService = postService;
            _categoryService = categoryService;
        }

        [Route("list/{categoryName}")]
        public async Task<IActionResult> List(string categoryName, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return NotFound();
            }

            var pageSize = AppSettings.PostListPageSize;
            var catResponse = await _categoryService.GetCategoryAsync(categoryName);
            if (!catResponse.IsSuccess)
            {
                return ServerError($"Unsuccessful response: {catResponse.Message}");
            }

            var cat = catResponse.Item;
            if (null == cat)
            {
                Logger.LogWarning($"{categoryName} is not found, returning NotFound.");
                return NotFound();
            }

            ViewBag.CategoryDisplayName = cat.DisplayName;
            ViewBag.CategoryName = cat.Title;
            ViewBag.CategoryDescription = cat.Note;

            var postCount = _categoryService.GetPostCountByCategoryId(cat.Id);
            var postList = await _postService.GetPagedPostsAsync(pageSize, page, cat.Id);

            var postsAsIPagedList = new StaticPagedList<PostListItem>(postList, page, pageSize, postCount);
            return View(postsAsIPagedList);
        }
    }
}