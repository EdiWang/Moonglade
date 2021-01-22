using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(MenuEditViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var request = new CreateMenuRequest
            {
                DisplayOrder = model.DisplayOrder,
                Icon = model.Icon,
                Title = model.Title,
                Url = model.Url,
                IsOpenInNewTab = model.IsOpenInNewTab
            };

            var response = await _menuService.CreateAsync(request);
            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _menuService.DeleteAsync(id);
            return Ok();
        }

        [HttpGet("edit/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var menu = await _menuService.GetAsync(id);
            if (null == menu) return NotFound();

            var model = new MenuEditViewModel
            {
                Id = menu.Id,
                DisplayOrder = menu.DisplayOrder,
                Icon = menu.Icon,
                Title = menu.Title,
                Url = menu.Url,
                IsOpenInNewTab = menu.IsOpenInNewTab
            };

            return Ok(model);
        }

        [HttpPost("edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit(MenuEditViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var request = new EditMenuRequest(model.Id)
            {
                Title = model.Title,
                DisplayOrder = model.DisplayOrder,
                Icon = model.Icon,
                Url = model.Url,
                IsOpenInNewTab = model.IsOpenInNewTab
            };

            await _menuService.UpdateAsync(request);
            return Ok();
        }
    }
}
