using System.Collections.Generic;
using Moonglade.Model;

namespace Moonglade.Web.Models
{
    public class CategoryManageViewModel
    {
        public CategoryEditViewModel CategoryEditViewModel { get; set; }

        public IReadOnlyList<Category> Categories { get; set; }

        public CategoryManageViewModel()
        {
            CategoryEditViewModel = new();
            Categories = new List<Category>();
        }
    }
}
