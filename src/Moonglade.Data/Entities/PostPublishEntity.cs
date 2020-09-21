using System;

namespace Moonglade.Data.Entities
{
    public class PostPublishEntity
    {
        public Guid PostId { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDeleted { get; set; }
        public virtual PostEntity Post { get; set; }
    }
}
