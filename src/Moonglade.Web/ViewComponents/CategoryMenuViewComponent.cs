using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.ViewComponents
{
    public class CategoryMenuViewComponent : MoongladeViewComponent
    {
        private readonly CategoryService _categoryService;

        public CategoryMenuViewComponent(
            ILogger<CategoryMenuViewComponent> logger,
            MoongladeDbContext context,
            IOptions<AppSettings> settings, CategoryService categoryService) : base(logger, context, settings)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            await Task.CompletedTask;
            var response = _categoryService.GetCategoryList();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }
            // should not block website
            return View(new List<CategoryInfo>());
        }
    }
}
