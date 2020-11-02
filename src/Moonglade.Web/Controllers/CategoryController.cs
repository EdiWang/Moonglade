using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ILogger<CategoryController> _logger;
        private readonly CategoryService _categoryService;
        private static string DataDirectory => AppDomain.CurrentDomain.GetData(Constants.DataDirectory)?.ToString();

        public CategoryController(ILogger<CategoryController> logger, CategoryService categoryService)
        {
            _logger = logger;
            _categoryService = categoryService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(CategoryEditViewModel model)
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

            return Ok(model);
        }

        [HttpGet("edit/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            return Ok(model);
        }

        [HttpPost("edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit(CategoryEditViewModel model)
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
            return Ok(model);
        }

        [HttpDelete("delete/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(id), "value is empty");
                return BadRequest(ModelState);
            }

            await _categoryService.DeleteAsync(id);
            DeleteOpmlFile();

            return Ok();
        }

        private void DeleteOpmlFile()
        {
            try
            {
                var path = Path.Join($"{DataDirectory}", $"{Constants.OpmlFileName}");
                System.IO.File.Delete(path);
                _logger.LogInformation("OPML file is deleted.");
            }
            catch (Exception e)
            {
                // Log the error and do not block the application
                _logger.LogError(e, "Error Delete OPML File.");
            }
        }
    }
}