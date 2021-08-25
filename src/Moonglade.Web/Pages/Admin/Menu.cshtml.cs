using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Menus;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages.Admin
{
    public class MenuModel : PageModel
    {
        private readonly IMenuService _menuService;

        [BindProperty]
        public IReadOnlyList<Menu> MenuItems { get; set; }

        public MenuModel(IMenuService menuService)
        {
            _menuService = menuService;
            MenuItems = new List<Menu>();
        }

        public async Task OnGet()
        {
            MenuItems = await _menuService.GetAllAsync();
        }
    }
}
