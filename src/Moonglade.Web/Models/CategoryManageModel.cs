using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moonglade.Model;

namespace Moonglade.Web.Models
{
    public class CategoryManageModel
    {
        public CategoryEditViewModel CategoryEditViewModel { get; set; }

        public IReadOnlyList<Model.Category> Categories { get; set; }

        public CategoryManageModel()
        {
            CategoryEditViewModel = new CategoryEditViewModel();
            Categories = new List<Category>();
        }
    }
}
