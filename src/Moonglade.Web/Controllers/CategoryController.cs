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
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _catService;

        public CategoryController(ICategoryService catService)
        {
            _catService = catService;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([NotEmpty] Guid id)
        {
            var cat = await _catService.Get(id);
            if (null == cat) return NotFound();

            return Ok(cat);
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(CategoryEditModel model)
        {
            await _catService.CreateAsync(model.DisplayName, model.RouteName, model.Note);
            return Created(string.Empty, model);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(CategoryEditModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update([NotEmpty] Guid id, CategoryEditModel model)
        {
            var request = new UpdateCatRequest(model.DisplayName, model.RouteName)
            {
                Note = model.Note
            };

            await _catService.UpdateAsync(id, request);
            return Ok(model);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            await _catService.DeleteAsync(id);
            return Ok();
        }
    }
}