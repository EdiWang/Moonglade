using System.Collections.Generic;
using Moonglade.Core;

namespace Moonglade.Web.Models
{
    public class CategoryManageModel
    {
        public CategoryEditModel CategoryEditViewModel { get; set; }

        public IReadOnlyList<Category> Categories { get; set; }

        public CategoryManageModel()
        {
            CategoryEditViewModel = new();
            Categories = new List<Category>();
        }
    }
}
