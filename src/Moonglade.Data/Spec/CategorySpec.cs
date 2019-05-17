using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class CategorySpec : BaseSpecification<Category>
    {
        public CategorySpec(string categoryName) : base(c => c.Title == categoryName)
        {
            
        }

        public CategorySpec(Guid categoryId) : base(c => c.Id == categoryId)
        {

        }
    }
}
