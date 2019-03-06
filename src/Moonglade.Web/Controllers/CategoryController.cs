using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
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

        public CategoryController(MoongladeDbContext context,
            ILogger<CategoryController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor, CategoryService categoryService, PostService postService)
            : base(context, logger, settings, configuration, accessor)
        {
            _postService = postService;
            _categoryService = categoryService;
        }

        [Route("list/{categoryName}")]
        public IActionResult List(string categoryName, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                return NotFound();
            }

            var pageSize = AppSettings.PostListPageSize;
            var catResponse = _categoryService.GetAllCategories();
            if (!catResponse.IsSuccess)
            {
                return ServerError("Unsuccessful response from _categoryService.GetAllCategories().");
            }

            var cat = catResponse.Item.Find(p => string.Compare(p.Title, categoryName, StringComparison.OrdinalIgnoreCase) == 0);
            if (null == cat)
            {
                Logger.LogWarning($"{categoryName} is not found, returning 404.");
                return NotFound();
            }

            ViewBag.CategoryDisplayName = cat.DisplayName;
            ViewBag.CategoryName = cat.Title;
            ViewBag.CategoryDescription = cat.Note;

            var postCount = _categoryService.GetPostCountByCategoryId(cat.Id);
            var postList = _postService.GetPagedPosts(pageSize, page, cat.Id);

            var postsAsIPagedList = new StaticPagedList<Post>(postList, page, pageSize, postCount);
            return View(postsAsIPagedList);
        }

        private void DeleteOpmlFile()
        {
            try
            {
                System.IO.File.Delete($"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\\opml.xml");
                Logger.LogInformation("OPML file is deleted.");
            }
            catch (Exception e)
            {
                // Log the error and do not block the application
                Logger.LogError(e, "Error Delete OPML File.");
            }
        }
    }
}