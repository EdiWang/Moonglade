using System;
using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class PostEntity
    {
        public PostEntity()
        {
            Comments = new HashSet<CommentEntity>();
            PostCategory = new HashSet<PostCategoryEntity>();
            Tags = new HashSet<TagEntity>();
        }

        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string PostContent { get; set; }
        public bool CommentEnabled { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public string ContentAbstract { get; set; }
        public string ContentLanguageCode { get; set; }
        public bool ExposedToSiteMap { get; set; }
        public bool IsFeedIncluded { get; set; }
        public DateTime? PubDateUtc { get; set; }
        public DateTime? LastModifiedUtc { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDeleted { get; set; }

        public virtual PostExtensionEntity PostExtension { get; set; }
        public virtual ICollection<CommentEntity> Comments { get; set; }
        public virtual ICollection<PostCategoryEntity> PostCategory { get; set; }
        public virtual ICollection<TagEntity> Tags { get; set; }
    }
}
