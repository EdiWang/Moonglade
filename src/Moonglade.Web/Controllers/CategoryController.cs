using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;
using X.PagedList;
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Web.Controllers
{
    [Route("category")]
    public class CategoryController : MoongladeController
    {
        private readonly PostService _postService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IMoongladeAudit _moongladeAudit;

        public CategoryController(
            ILogger<CategoryController> logger,
            IOptions<AppSettings> settings,
            CategoryService categoryService,
            PostService postService,
            IBlogConfig blogConfig,
            IMoongladeAudit moongladeAudit)
            : base(logger, settings)
        {
            _postService = postService;
            _categoryService = categoryService;

            _blogConfig = blogConfig;
            _moongladeAudit = moongladeAudit;
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
                    return View(new CategoryManageModel());
                }
                return View(new CategoryManageModel { Categories = allCats.Item });
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(Manage)}()");

                ViewBag.HasError = true;
                ViewBag.ErrorMessage = e.Message;
                return View(new CategoryManageModel());
            }
        }

        [Authorize]
        [HttpPost("manage/create")]
        public IActionResult Create(CategoryEditViewModel model)
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

                    var response = _categoryService.CreateCategory(request);
                    if (response.IsSuccess)
                    {
                        DeleteOpmlFile();

                        _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CategoryCreated, $"Category '{request.Title}' is created");
                        return Json(response);
                    }

                    Logger.LogError($"Create category failed: {response.Message}");
                    ModelState.AddModelError("", response.Message);
                    return BadRequest("Invalid ModelState");
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
        [HttpGet("edit/{id:guid}")]
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

                return View("CreateOrEdit", model);
            }

            return NotFound();
        }

        [Authorize]
        [HttpPost("edit")]
        public IActionResult Edit(CategoryEditViewModel model)
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

                    var response = _categoryService.UpdateCategory(request);

                    if (response.IsSuccess)
                    {
                        DeleteOpmlFile();

                        _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CategoryUpdated, $"Category '{model.Id}' is updated");
                        return RedirectToAction(nameof(Manage));
                    }

                    Logger.LogError($"Edit category failed: {response.Message}");
                    ModelState.AddModelError("", response.Message);
                    return View("CreateOrEdit", model);
                }

                return View("CreateOrEdit", model);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Create Category.");

                ModelState.AddModelError("", e.Message);
                return View("CreateOrEdit", model);
            }
        }

        [Authorize]
        [HttpPost("manage/delete")]
        public IActionResult Delete(Guid id)
        {
            try
            {
                Logger.LogInformation($"Deleting category id: {id}");
                var response = _categoryService.Delete(id);
                if (response.IsSuccess)
                {
                    DeleteOpmlFile();

                    _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CategoryDeleted, $"Category '{id}' is deleted.");
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