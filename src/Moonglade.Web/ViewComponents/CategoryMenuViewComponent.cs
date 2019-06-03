using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class CategoryMenuViewComponent : MoongladeViewComponent
    {
        private readonly CategoryService _categoryService;

        public CategoryMenuViewComponent(
            ILogger<CategoryMenuViewComponent> logger,
            IOptions<AppSettings> settings, CategoryService categoryService) : base(logger, settings)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var response = await _categoryService.GetCategoryListAsync();
            return View(response.IsSuccess ? response.Item : new List<Category>());
        }
    }
}
