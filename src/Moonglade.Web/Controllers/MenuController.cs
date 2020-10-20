using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("navmenu")]
    public class MenuController : BlogController
    {
        private readonly MenuService _menuService;

        public MenuController(
            ILogger<ControllerBase> logger,
            IOptions<AppSettings> settings,
            MenuService menuService) : base(logger, settings)
        {
            _menuService = menuService;
        }

        [HttpGet("manage")]
        public async Task<IActionResult> Manage([FromServices] MenuService menuService)
        {
            var menus = await menuService.GetAllAsync();
            var model = new MenuManageViewModel
            {
                MenuItems = menus
            };

            return View("~/Views/Admin/ManageMenu.cshtml", model);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(MenuEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest("Invalid ModelState");

                var request = new CreateMenuRequest
                {
                    DisplayOrder = model.DisplayOrder,
                    Icon = model.Icon,
                    Title = model.Title,
                    Url = model.Url,
                    IsOpenInNewTab = model.IsOpenInNewTab
                };

                var response = await _menuService.CreateAsync(request);
                return Json(response);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Create Nav Menu.");

                ModelState.AddModelError("", e.Message);
                return ServerError(e.Message);
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                Logger.LogInformation($"Deleting Menu id: {id}");
                await _menuService.DeleteAsync(id);
                return Json(id);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Delete Menu.");
                return ServerError();
            }
        }

        [HttpGet("edit/{id:guid}")]
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

            return Json(model);
        }

        [Authorize]
        [HttpPost("edit")]
        public async Task<IActionResult> Edit(MenuEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                var request = new EditMenuRequest(model.Id)
                {
                    Title = model.Title,
                    DisplayOrder = model.DisplayOrder,
                    Icon = model.Icon,
                    Url = model.Url,
                    IsOpenInNewTab = model.IsOpenInNewTab
                };

                var response = await _menuService.UpdateAsync(request);
                return Json(response);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Editing Menu.");

                ModelState.AddModelError("", e.Message);
                return ServerError();
            }
        }
    }
}
