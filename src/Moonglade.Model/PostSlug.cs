using System;

namespace Moonglade.Model
{
    public class PostSlug : Post
    {
        public int Hits { get; set; }
        public int Likes { get; set; }
        public int CommentCount { get; set; }
    }
}