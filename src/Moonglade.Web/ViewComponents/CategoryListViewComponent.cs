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

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _categoryService.GetAllAsync();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            ViewBag.ComponentErrorMessage = response.Message;
            return View("~/Views/Shared/ComponentError.cshtml");
        }
    }
}
