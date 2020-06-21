using System;
using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class PostEntity
    {
        public PostEntity()
        {
            Comment = new HashSet<CommentEntity>();
            PostCategory = new HashSet<PostCategoryEntity>();
            PostTag = new HashSet<PostTagEntity>();
        }

        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string PostContent { get; set; }
        public bool CommentEnabled { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public string ContentAbstract { get; set; }

        public virtual PostExtensionEntity PostExtension { get; set; }
        public virtual PostPublishEntity PostPublish { get; set; }
        public virtual ICollection<CommentEntity> Comment { get; set; }
        public virtual ICollection<PostCategoryEntity> PostCategory { get; set; }
        public virtual ICollection<PostTagEntity> PostTag { get; set; }
    }
}
