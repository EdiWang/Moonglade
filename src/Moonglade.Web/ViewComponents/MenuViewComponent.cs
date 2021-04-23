using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Menus;

namespace Moonglade.Web.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly IMenuService _menuService;
        private readonly IBlogCache _cache;

        public MenuViewComponent(IMenuService menuService, IBlogCache cache)
        {
            _menuService = menuService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var menus = await _cache.GetOrCreateAsync(CacheDivision.General, "menu", async entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(20);

                    var items = await _menuService.GetAllAsync();
                    return items;
                });

                return View(menus);
            }
            catch (Exception e)
            {
                return Content(e.Message);
            }
        }
    }
}
