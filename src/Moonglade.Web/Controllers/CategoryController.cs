using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Route("category")]
    public class CategoryController : MoongladeController
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

        [Route("list/{categoryName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
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

        [Authorize]
        [HttpGet("manage")]
        public async Task<IActionResult> Manage()
        {
            try
            {
                var allCats = await _categoryService.GetAllCategoriesAsync();
                if (!allCats.IsSuccess)
                {
                    ViewBag.HasError = true;
                    ViewBag.ErrorMessage = allCats.Message;
                    return View(new CategoryManageViewModel());
                }
                return View(new CategoryManageViewModel { Categories = allCats.Item });
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(Manage)}()");

                ViewBag.HasError = true;
                ViewBag.ErrorMessage = e.Message;
                return View(new CategoryManageViewModel());
            }
        }

        [Authorize]
        [HttpPost("manage/create")]
        public async Task<IActionResult> Create(CategoryEditViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var request = new CreateCategoryRequest
                    {
                        Title = model.Name,
                        Note = model.Note,
                        DisplayName = model.DisplayName
                    };

                    var response = await _categoryService.CreateCategoryAsync(request);
                    if (response.IsSuccess)
                    {
                        DeleteOpmlFile();
                        return Json(response);
                    }

                    Logger.LogError($"Create category failed: {response.Message}");
                    ModelState.AddModelError("", response.Message);
                    return BadRequest("Create category failed");
                }

                return BadRequest("Invalid ModelState");
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
            var r = await _categoryService.GetCategoryAsync(id);
            if (r.IsSuccess && null != r.Item)
            {
                var model = new CategoryEditViewModel
                {
                    Id = r.Item.Id,
                    DisplayName = r.Item.DisplayName,
                    Name = r.Item.Name,
                    Note = r.Item.Note
                };

                return Json(model);
            }

            return NotFound();
        }

        [Authorize]
        [HttpPost("manage/edit")]
        public async Task<IActionResult> Edit(CategoryEditViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var request = new EditCategoryRequest(model.Id)
                    {
                        Title = model.Name,
                        Note = model.Note,
                        DisplayName = model.DisplayName
                    };

                    var response = await _categoryService.UpdateCategoryAsync(request);

                    if (response.IsSuccess)
                    {
                        DeleteOpmlFile();
                        return Json(response);
                    }

                    Logger.LogError($"Edit category failed: {response.Message}");
                    ModelState.AddModelError("", response.Message);
                    return BadRequest();
                }

                return BadRequest();
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
                var response = await _categoryService.DeleteAsync(id);
                if (response.IsSuccess)
                {
                    DeleteOpmlFile();
                    return Json(id);
                }

                Logger.LogError(response.Message);
                return ServerError();
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