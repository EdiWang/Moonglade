using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Menus;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    //[Authorize]
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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(MenuEditViewModel model)
        {
            if (null == model.SubMenuEditViewModels)
            {
                var request = new UpdateMenuRequest
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
            else
            {
                var topMenuRequest = new UpdateMenuRequest
                {
                    DisplayOrder = model.DisplayOrder,
                    Title = model.Title
                };

                var subMenuRequests = model.SubMenuEditViewModels
                    .Select(p => new UpdateSubMenuRequest
                    {
                        Title = p.Title,
                        Url = p.Url,
                        IsOpenInNewTab = p.IsOpenInNewTab
                    }).ToArray();

                var response = await _menuService.CreateAsync(topMenuRequest, subMenuRequests);
                return Ok(response);
            }
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();

            await _menuService.DeleteAsync(id);
            return Ok();
        }

        [HttpGet("edit/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var menu = await _menuService.GetAsync(id);
            if (null == menu) return NotFound();

            var model = new MenuEditViewModel
            {
                Id = menu.Id,
                DisplayOrder = menu.DisplayOrder,
                Icon = menu.Icon,
                Title = menu.Title,
                Url = menu.Url,
                IsOpenInNewTab = menu.IsOpenInNewTab,
                SubMenuEditViewModels = menu.SubMenus.Select(p => new SubMenuEditViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Url = p.Url,
                    IsOpenInNewTab = p.IsOpenInNewTab
                }).ToList()
            };

            return Ok(model);
        }

        [IgnoreAntiforgeryToken]
        [HttpPost("edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit(MenuEditViewModel model)
        {
            if (null == model.SubMenuEditViewModels)
            {
                var request = new UpdateMenuRequest
                {
                    Title = model.Title,
                    DisplayOrder = model.DisplayOrder,
                    Icon = model.Icon,
                    Url = model.Url,
                    IsOpenInNewTab = model.IsOpenInNewTab
                };

                await _menuService.UpdateAsync(model.Id, request);
                return Ok();
            }
            else
            {
                var topMenuRequest = new UpdateMenuRequest
                {
                    DisplayOrder = model.DisplayOrder,
                    Title = model.Title
                };

                var subMenuRequests = model.SubMenuEditViewModels
                    .Select(p => new UpdateSubMenuRequest
                    {
                        Title = p.Title,
                        Url = p.Url,
                        IsOpenInNewTab = p.IsOpenInNewTab
                    }).ToArray();

                var response = await _menuService.UpdateAsync(model.Id, topMenuRequest, subMenuRequests);
                return Ok(response);
            }
        }
    }
}
