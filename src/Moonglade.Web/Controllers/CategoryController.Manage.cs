using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Moonglade.Web.Models;
using Newtonsoft.Json;

namespace Moonglade.Web.Controllers
{
    public partial class CategoryController
    {
        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            try
            {
                var allCats = await _categoryService.GetAllCategoriesAsync();
                if (!allCats.IsSuccess)
                {
                    ViewBag.HasError = true;
                    ViewBag.ErrorMessage = allCats.Message;
                    return View(new List<Category>());
                }
                return View(allCats.Item);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(Manage)}()");

                ViewBag.HasError = true;
                ViewBag.ErrorMessage = e.Message;
                return View(new List<Category>());
            }
        }

        [Authorize]
        [Route("create")]
        public IActionResult Create()
        {
            var model = new CategoryEditViewModel();
            return View("CreateOrEdit", model);
        }

        [HttpPost]
        [Authorize]
        [Route("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryEditViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var category = new Category
                    {
                        Id = Guid.NewGuid(),
                        Title = model.Name,
                        Note = model.Note,
                        DisplayName = model.DisplayName
                    };

                    var catJson = JsonConvert.SerializeObject(category);
                    Logger.LogInformation($"Creating new category: {catJson}");

                    var response = _categoryService.CreateCategory(category);
                    if (response.IsSuccess)
                    {
                        DeleteOpmlFile();
                        return RedirectToAction(nameof(Manage));
                    }

                    Logger.LogError($"Create category failed: {response.Message}");
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
        [Route("edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var r = await _categoryService.GetCategoryAsync(id);
            if (r.IsSuccess && null != r.Item)
            {
                var model = new CategoryEditViewModel
                {
                    Id = r.Item.Id,
                    DisplayName = r.Item.DisplayName,
                    Name = r.Item.Title,
                    Note = r.Item.Note,
                };

                return View("CreateOrEdit", model);
            }

            return NotFound();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("edit")]
        public IActionResult Edit(CategoryEditViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var category = new Category
                    {
                        Id = model.Id,
                        Title = model.Name,
                        Note = model.Note,
                        DisplayName = model.DisplayName
                    };

                    var response = _categoryService.UpdateCategory(category);

                    if (response.IsSuccess)
                    {
                        DeleteOpmlFile();
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
        [Route("delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _categoryService.GetCategoryAsync(id);
            if (r.IsSuccess && null != r.Item)
            {
                var model = new CategoryEditViewModel
                {
                    Id = r.Item.Id,
                    DisplayName = r.Item.DisplayName,
                    Name = r.Item.Title,
                    Note = r.Item.Note
                };

                return View(model);
            }

            return NotFound();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("delete")]
        public IActionResult ConfirmDelete(Guid id)
        {
            try
            {
                Logger.LogInformation($"Deleting category id: {id}");
                var response = _categoryService.Delete(id);
                if (response.IsSuccess)
                {
                    DeleteOpmlFile();
                    return RedirectToAction(nameof(Manage));
                }

                Logger.LogError(response.Message);

                return Content(response.Message);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Delete Category.");
                return Content(e.Message);
            }
        }

        private void DeleteOpmlFile()
        {
            try
            {
                System.IO.File.Delete($@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpmlFileName}");
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