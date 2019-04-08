using System;
using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class Post
    {
        public Post()
        {
            Comment = new HashSet<Comment>();
            PostCategory = new HashSet<PostCategory>();
            PostTag = new HashSet<PostTag>();
        }

        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string PostContent { get; set; }
        public bool CommentEnabled { get; set; }
        public DateTime? CreateOnUtc { get; set; }
        public string ContentAbstract { get; set; }

        public virtual PostExtension PostExtension { get; set; }
        public virtual PostPublish PostPublish { get; set; }
        public virtual ICollection<Comment> Comment { get; set; }
        public virtual ICollection<PostCategory> PostCategory { get; set; }
        public virtual ICollection<PostTag> PostTag { get; set; }
    }
}
