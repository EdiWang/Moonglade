using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages.Admin
{
    public class CategoryModel : PageModel
    {
        private readonly ICategoryService _categoryService;

        public EditCategoryRequest EditCategoryRequest { get; set; }

        public IReadOnlyList<Category> Categories { get; set; }

        public CategoryModel(ICategoryService categoryService)
        {
            _categoryService = categoryService;
            EditCategoryRequest = new();
        }

        public async Task OnGet()
        {
            Categories = await _categoryService.GetAllAsync();
        }
    }
}
