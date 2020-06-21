using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class TagEntity
    {
        public TagEntity()
        {
            PostTag = new HashSet<PostTagEntity>();
        }

        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string NormalizedName { get; set; }

        public virtual ICollection<PostTagEntity> PostTag { get; set; }
    }
}
