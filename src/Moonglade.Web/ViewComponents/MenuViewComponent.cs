using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly IMenuService _menuService;

        public MenuViewComponent(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var response = await _menuService.GetAllAsync();
                return View(response);
            }
            catch (Exception e)
            {
                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
