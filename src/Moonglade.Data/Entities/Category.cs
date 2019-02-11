using System;
using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class Category
    {
        public Category()
        {
            PostCategory = new HashSet<PostCategory>();
        }

        public Guid Id { get; set; }
        public string Title { get; set; }
        public string DisplayName { get; set; }
        public string Note { get; set; }

        public virtual ICollection<PostCategory> PostCategory { get; set; }
    }
}
