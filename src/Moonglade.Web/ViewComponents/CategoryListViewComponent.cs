using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class CategoryListViewComponent : ViewComponent
    {
        private readonly CategoryService _categoryService;

        public CategoryListViewComponent(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
        {
            try
            {
                var cats = await _categoryService.GetAllAsync();
                return isMenu ? View("CatMenu", cats) : View(cats);
            }
            catch (Exception e)
            {
                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
