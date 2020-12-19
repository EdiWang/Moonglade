using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly CategoryService _catService;

        public CategoryController(CategoryService catService)
        {
            _catService = catService;
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

            await _catService.CreateAsync(request);
            return Ok(model);
        }

        [HttpGet("edit/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var cat = await _catService.GetAsync(id);
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

            await _catService.UpdateAsync(request);
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

            await _catService.DeleteAsync(id);
            return Ok();
        }
    }
}