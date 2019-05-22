using System;
using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class CategoryEntity
    {
        public CategoryEntity()
        {
            PostCategory = new HashSet<PostCategoryEntity>();
        }

        public Guid Id { get; set; }
        public string Title { get; set; }
        public string DisplayName { get; set; }
        public string Note { get; set; }

        public virtual ICollection<PostCategoryEntity> PostCategory { get; set; }
    }
}
