using System;
using System.Collections.Generic;

namespace Moonglade.Model
{
    public class PostSlugModel
    {
        public string Title { get; set; }
        public string Abstract { get; set; }
        public DateTime PubDateUtc { get; set; }
        public DateTime? LastModifyOnUtc { get; set; }
        public string Content { get; set; }
        public int Hits { get; set; }
        public int Likes { get; set; }
        public Guid PostId { get; set; }
        public bool CommentEnabled { get; set; }
        public int CommentCount { get; set; }
        public bool IsExposedToSiteMap { get; set; }

        public IList<CategoryInfo> Categories { get; set; }
        public IList<Tag> Tags { get; set; }
    }
}