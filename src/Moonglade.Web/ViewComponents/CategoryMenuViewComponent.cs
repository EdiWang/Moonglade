using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;

namespace Moonglade.Web.ViewComponents
{
    public class CategoryMenuViewComponent : MoongladeViewComponent
    {
        private readonly CategoryService _categoryService;

        public CategoryMenuViewComponent(
            ILogger<CategoryMenuViewComponent> logger, CategoryService categoryService) : base(logger)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _categoryService.GetCategoriesAsync();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            ViewBag.ComponentErrorMessage = response.Message;
            return View("~/Views/Shared/ComponentError.cshtml");
        }
    }
}
