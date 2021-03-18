using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Web.Models;

namespace Moonglade.Web.Pages.Admin
{
    public class CategoryModel : PageModel
    {
        private readonly ICategoryService _categoryService;

        public CategoryEditModel CategoryEditViewModel { get; set; }

        public IReadOnlyList<Category> Categories { get; set; }

        public CategoryModel(ICategoryService categoryService)
        {
            _categoryService = categoryService;
            CategoryEditViewModel = new();
        }

        public async Task OnGet()
        {
            Categories = await _categoryService.GetAll();
        }
    }
}
