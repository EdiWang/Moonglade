using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Route("category")]
    public class CategoryController : BlogController
    {
        private readonly PostService _postService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogCache _blogCache;

        public CategoryController(
            ILogger<CategoryController> logger,
            IOptions<AppSettings> settings,
            CategoryService categoryService,
            PostService postService,
            IBlogConfig blogConfig,
            IBlogCache blogCache)
            : base(logger, settings)
        {
            _postService = postService;
            _categoryService = categoryService;

            _blogConfig = blogConfig;
            _blogCache = blogCache;
        }

        [Route("list/{routeName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> List(string routeName, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

            var pageSize = _blogConfig.ContentSettings.PostListPageSize;
            var cat = await _categoryService.GetAsync(routeName);

            if (null == cat)
            {
                Logger.LogWarning($"Category '{routeName}' not found.");
                return NotFound();
            }

            ViewBag.CategoryDisplayName = cat.DisplayName;
            ViewBag.CategoryRouteName = cat.RouteName;
            ViewBag.CategoryDescription = cat.Note;

            var postCount = _blogCache.GetOrCreate(CacheDivision.PostCountCategory, cat.Id.ToString(),
                entry => _postService.CountByCategoryId(cat.Id));

            var postList = await _postService.GetPagedPostsAsync(pageSize, page, cat.Id);

            var postsAsIPagedList = new StaticPagedList<PostListEntry>(postList, page, pageSize, postCount);
            return View(postsAsIPagedList);
        }

        [Authorize]
        [HttpGet("manage")]
        public async Task<IActionResult> Manage()
        {
            string viewLocation = "~/Views/Admin/ManageCategory.cshtml";

            try
            {
                var allCats = await _categoryService.GetAllAsync();
                return View(viewLocation, new CategoryManageViewModel { Categories = allCats });
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(Manage)}()");

                ViewBag.HasError = true;
                ViewBag.ErrorMessage = e.Message;
                return View(viewLocation, new CategoryManageViewModel());
            }
        }

        [Authorize]
        [HttpPost("manage/create")]
        public async Task<IActionResult> Create(CategoryEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest("Invalid ModelState");

                var request = new CreateCategoryRequest
                {
                    RouteName = model.RouteName,
                    Note = model.Note,
                    DisplayName = model.DisplayName
                };

                await _categoryService.CreateAsync(request);
                DeleteOpmlFile();

                return Json(model);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Create Category.");

                ModelState.AddModelError("", e.Message);
                return ServerError(e.Message);
            }
        }

        [Authorize]
        [HttpGet("manage/edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var cat = await _categoryService.GetAsync(id);
            if (null == cat) return NotFound();

            var model = new CategoryEditViewModel
            {
                Id = cat.Id,
                DisplayName = cat.DisplayName,
                RouteName = cat.RouteName,
                Note = cat.Note
            };

            return Json(model);
        }

        [Authorize]
        [HttpPost("manage/edit")]
        public async Task<IActionResult> Edit(CategoryEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                var request = new EditCategoryRequest(model.Id)
                {
                    RouteName = model.RouteName,
                    Note = model.Note,
                    DisplayName = model.DisplayName
                };

                await _categoryService.UpdateAsync(request);

                DeleteOpmlFile();
                return Json(model);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Create Category.");

                ModelState.AddModelError("", e.Message);
                return ServerError();
            }
        }

        [Authorize]
        [HttpPost("manage/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                Logger.LogInformation($"Deleting category id: {id}");
                await _categoryService.DeleteAsync(id);
                DeleteOpmlFile();

                return Json(id);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Delete Category.");
                return ServerError();
            }
        }

        private void DeleteOpmlFile()
        {
            try
            {
                var path = Path.Join($"{SiteDataDirectory}", $"{Constants.OpmlFileName}");
                System.IO.File.Delete(path);
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