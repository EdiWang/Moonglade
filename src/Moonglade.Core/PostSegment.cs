using System;

namespace Moonglade.Core
{
    public class PostSegment
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public DateTime? PubDateUtc { get; set; }
        public DateTime CreateTimeUtc { get; set; }
        public bool IsPublished { get; set; }
        public int Hits { get; set; }
        public bool IsDeleted { get; set; }
    }
}
