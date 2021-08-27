using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Caching.Filters;
using Moonglade.Menus;
using Moonglade.Web.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;
        private readonly IMediator _mediator;

        public MenuController(IMenuService menuService, IMediator mediator)
        {
            _menuService = menuService;
            _mediator = mediator;
        }

        [HttpPost]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "menu" })]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(MenuEditViewModel model)
        {
            var request = new UpdateMenuRequest
            {
                DisplayOrder = model.DisplayOrder.GetValueOrDefault(),
                Icon = model.Icon,
                Title = model.Title,
                Url = model.Url,
                IsOpenInNewTab = model.IsOpenInNewTab
            };

            if (null != model.SubMenus)
            {
                var subMenuRequests = model.SubMenus
                    .Select(p => new UpdateSubMenuRequest
                    {
                        Title = p.Title,
                        Url = p.Url,
                        IsOpenInNewTab = p.IsOpenInNewTab
                    }).ToArray();

                request.SubMenus = subMenuRequests;
            }

            var response = await _menuService.CreateAsync(request);
            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "menu" })]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            await _menuService.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("edit/{id:guid}")]
        [ProducesResponseType(typeof(MenuEditViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Edit([NotEmpty] Guid id)
        {
            var menu = await _mediator.Send(new GetMenuQuery(id));
            if (null == menu) return NotFound();

            var model = new MenuEditViewModel
            {
                Id = menu.Id,
                DisplayOrder = menu.DisplayOrder,
                Icon = menu.Icon,
                Title = menu.Title,
                Url = menu.Url,
                IsOpenInNewTab = menu.IsOpenInNewTab,
                SubMenus = menu.SubMenus.Select(p => new SubMenuEditViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Url = p.Url,
                    IsOpenInNewTab = p.IsOpenInNewTab
                }).ToArray()
            };

            return Ok(model);
        }

        [HttpPut("edit")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "menu" })]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Edit(MenuEditViewModel model)
        {
            var request = new UpdateMenuRequest
            {
                Title = model.Title,
                DisplayOrder = model.DisplayOrder.GetValueOrDefault(),
                Icon = model.Icon,
                Url = model.Url,
                IsOpenInNewTab = model.IsOpenInNewTab
            };

            if (null != model.SubMenus)
            {
                var subMenuRequests = model.SubMenus
                    .Select(p => new UpdateSubMenuRequest
                    {
                        Title = p.Title,
                        Url = p.Url,
                        IsOpenInNewTab = p.IsOpenInNewTab
                    }).ToArray();

                request.SubMenus = subMenuRequests;
            }

            await _menuService.UpdateAsync(model.Id, request);
            return NoContent();
        }
    }
}
