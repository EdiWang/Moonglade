using System;

namespace Moonglade.Data.Entities
{
    public class PostCategoryEntity
    {
        public Guid PostId { get; set; }
        public Guid CategoryId { get; set; }

        public virtual CategoryEntity Category { get; set; }
        public virtual PostEntity Post { get; set; }
    }
}
