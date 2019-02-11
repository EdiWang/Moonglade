using System;

namespace Moonglade.Data.Entities
{
    public class PostTag
    {
        public Guid PostId { get; set; }
        public int TagId { get; set; }

        public virtual Post Post { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
