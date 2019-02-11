using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class Tag
    {
        public Tag()
        {
            PostTag = new HashSet<PostTag>();
        }

        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string NormalizedName { get; set; }

        public virtual ICollection<PostTag> PostTag { get; set; }
    }
}
