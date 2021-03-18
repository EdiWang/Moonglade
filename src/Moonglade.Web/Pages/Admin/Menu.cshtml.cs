using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Menus;
using Moonglade.Web.Models;

namespace Moonglade.Web.Pages.Admin
{
    public class MenuModel : PageModel
    {
        private readonly IMenuService _menuService;

        [BindProperty]
        public MenuEditViewModel MenuEditViewModel { get; set; }

        [BindProperty]
        public IReadOnlyList<Menu> MenuItems { get; set; }

        public MenuModel(IMenuService menuService)
        {
            _menuService = menuService;

            MenuEditViewModel = new();
            MenuItems = new List<Menu>();
        }

        public async Task OnGet()
        {
            MenuItems = await _menuService.GetAllAsync();
        }
    }
}
