using System;

namespace Moonglade.Model
{
    public class PostSlug : Post
    {
        public DateTime? LastModifyOnUtc { get; set; }
        public string Content { get; set; }
        public int Hits { get; set; }
        public int Likes { get; set; }
        public int CommentCount { get; set; }
    }
}