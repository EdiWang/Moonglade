using System;

namespace Moonglade.Data.Entities
{
    public class PostExtension
    {
        public Guid PostId { get; set; }
        public int Hits { get; set; }
        public int? Likes { get; set; }

        public virtual Post Post { get; set; }
    }
}
