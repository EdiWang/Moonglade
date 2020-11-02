using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("category")]
    public class CategoryController : BlogController
    {
        private readonly CategoryService _categoryService;

        public CategoryController(ILogger<CategoryController> logger, CategoryService categoryService)
            : base(logger)
        {
            _categoryService = categoryService;
        }

        [HttpPost("manage/create")]
        public async Task<IActionResult> Create(CategoryEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

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

        [HttpPost("manage/edit")]
        public async Task<IActionResult> Edit(CategoryEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

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
                var path = Path.Join($"{DataDirectory}", $"{Constants.OpmlFileName}");
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