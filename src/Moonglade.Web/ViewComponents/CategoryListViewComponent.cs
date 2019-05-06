using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class CategoryListViewComponent : MoongladeViewComponent
    {
        private readonly CategoryService _categoryService;

        public CategoryListViewComponent(
            ILogger<CategoryMenuViewComponent> logger,
            IOptions<AppSettings> settings, CategoryService categoryService) : base(logger, settings)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _categoryService.GetCategoryListAsync();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            ViewBag.ComponentErrorMessage = response.Message;
            return View("~/Views/Shared/ComponentError.cshtml");
        }
    }
}
