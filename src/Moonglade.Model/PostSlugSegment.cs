using System;

namespace Moonglade.Model
{
    public class PostSlugSegment
    {
        public string Title { get; set; }
        public DateTime PubDateUtc { get; set; }
        public DateTime? LastModifyOnUtc { get; set; }
        public string[] Categories { get; set; }
        public string[] Tags { get; set; }
    }
}