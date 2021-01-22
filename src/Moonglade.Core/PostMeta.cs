using System;

namespace Moonglade.Core
{
    public class PostMeta
    {
        public string Title { get; set; }
        public DateTime PubDateUtc { get; set; }
        public DateTime? UpdatedTimeUtc { get; set; }
        public string[] Categories { get; set; }
        public string[] Tags { get; set; }
    }
}