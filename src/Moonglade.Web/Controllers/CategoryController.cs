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
using Moonglade.Data;
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
            var cat = await _catService.GetAsync(id);
            if (null == cat) return NotFound();

            return Ok(cat);
        }

        [HttpGet("list")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        [ProducesResponseType(typeof(IReadOnlyList<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> List()
        {
            var cats = await _catService.GetAllAsync();
            return Ok(cats);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(EditCategoryRequest model)
        {
            await _catService.CreateAsync(model.DisplayName, model.RouteName, model.Note);
            return Created(string.Empty, model);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(EditCategoryRequest), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([NotEmpty] Guid id, EditCategoryRequest model)
        {
            var oc = await _catService.UpdateAsync(id, model.DisplayName, model.RouteName, model.Note);
            if (oc == OperationCode.ObjectNotFound) return NotFound();

            return Ok(model);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            await _catService.DeleteAsync(id);
            return NoContent();
        }
    }
}