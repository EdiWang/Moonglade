using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Utils;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [AppendAppVersion]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _catService;

        public CategoryController(ICategoryService catService)
        {
            _catService = catService;
        }

        [HttpGet("list")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> List()
        {
            var cats = await _catService.GetAll();
            return Ok(cats);
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(CategoryEditModel model)
        {
            var request = new UpdateCatRequest
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
            var cat = await _catService.Get(id);
            if (null == cat) return NotFound();

            var model = new CategoryEditModel
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
        public async Task<IActionResult> Edit(CategoryEditModel model)
        {
            var request = new UpdateCatRequest
            {
                RouteName = model.RouteName,
                Note = model.Note,
                DisplayName = model.DisplayName
            };

            await _catService.UpdateAsync(model.Id, request);
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
                return BadRequest(ModelState.CombineErrorMessages());
            }

            await _catService.DeleteAsync(id);
            return Ok();
        }
    }
}