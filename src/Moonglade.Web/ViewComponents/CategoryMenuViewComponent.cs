using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly CategoryService _categoryService;

        public CategoryMenuViewComponent(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var cats = await _categoryService.GetAllAsync();
                return View(cats);
            }
            catch (Exception e)
            {
                ViewBag.ComponentErrorMessage = e.Message;
                return View("~/Views/Shared/ComponentError.cshtml");
            }
        }
    }
}
