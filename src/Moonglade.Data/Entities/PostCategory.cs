using System;

namespace Moonglade.Data.Entities
{
    public class PostCategory
    {
        public Guid PostId { get; set; }
        public Guid CategoryId { get; set; }

        public virtual Category Category { get; set; }
        public virtual Post Post { get; set; }
    }
}
