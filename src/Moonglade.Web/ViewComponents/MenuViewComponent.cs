using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class MenuViewComponent : BlogViewComponent
    {
        private readonly MenuService _menuService;

        public MenuViewComponent(
            ILogger<MenuViewComponent> logger, MenuService menuService) : base(logger)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _menuService.GetAllAsync();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            ViewBag.ComponentErrorMessage = response.Message;
            return View("~/Views/Shared/ComponentError.cshtml");
        }
    }
}
