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
    public class NavMenuController : MoongladeController
    {
        private readonly MenuService _menuService;

        public NavMenuController(
            ILogger<ControllerBase> logger,
            IOptions<AppSettings> settings,
            MenuService menuService) : base(logger, settings)
        {
            _menuService = menuService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(NavMenuEditViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var request = new CreateMenuRequest
                    {
                        DisplayOrder = model.DisplayOrder,
                        Icon = model.Icon,
                        Title = model.Title,
                        Url = model.Url,
                        IsOpenInNewTab = model.IsOpenInNewTab
                    };

                    var response = await _menuService.CreateMenuAsync(request);
                    if (response.IsSuccess)
                    {
                        return Json(response);
                    }

                    Logger.LogError($"Create menu failed: {response.Message}");
                    ModelState.AddModelError("", response.Message);
                    return BadRequest("Create Menu Failed.");
                }
                return BadRequest("Invalid ModelState");
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
                var response = await _menuService.DeleteAsync(id);
                if (response.IsSuccess)
                {
                    return Json(id);
                }

                Logger.LogError(response.Message);
                return ServerError();
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
            var r = await _menuService.GetMenuAsync(id);
            if (r.IsSuccess && null != r.Item)
            {
                var model = new NavMenuEditViewModel
                {
                    Id = r.Item.Id,
                    DisplayOrder = r.Item.DisplayOrder,
                    Icon = r.Item.Icon,
                    Title = r.Item.Title,
                    Url = r.Item.Url,
                    IsOpenInNewTab = r.Item.IsOpenInNewTab
                };

                return Json(model);
            }

            return NotFound();
        }

        [Authorize]
        [HttpPost("edit")]
        public async Task<IActionResult> Edit(NavMenuEditViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var request = new EditMenuRequest(model.Id)
                    {
                        Title = model.Title,
                        DisplayOrder = model.DisplayOrder,
                        Icon = model.Icon,
                        Url = model.Url,
                        IsOpenInNewTab = model.IsOpenInNewTab
                    };

                    var response = await _menuService.UpdateMenuAsync(request);

                    if (response.IsSuccess)
                    {
                        return Json(response);
                    }

                    Logger.LogError($"Edit menu failed: {response.Message}");
                    ModelState.AddModelError("", response.Message);
                    return BadRequest();
                }

                return BadRequest();
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
