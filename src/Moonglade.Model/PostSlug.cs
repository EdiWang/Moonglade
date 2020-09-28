using System;

namespace Moonglade.Model
{
    public class PostSlug : Post
    {
        public string Abstract { get; set; }
        public DateTime? LastModifyOnUtc { get; set; }
        public string Content { get; set; }
        public int Hits { get; set; }
        public int Likes { get; set; }
        public Guid PostId { get; set; }
        public int CommentCount { get; set; }
        public bool IsExposedToSiteMap { get; set; }
        public string LangCode { get; set; }
    }
}