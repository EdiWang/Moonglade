using System;

namespace Moonglade.Data.Entities
{
    public class PostExtensionEntity
    {
        public Guid PostId { get; set; }
        public int Hits { get; set; }
        public int Likes { get; set; }

        public virtual PostEntity Post { get; set; }
    }
}
