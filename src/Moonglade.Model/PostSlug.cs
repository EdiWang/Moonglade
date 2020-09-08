using System;
using System.Collections.Generic;

namespace Moonglade.Model
{
    public class PostSlug
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
        public string LangCode { get; set; }
        public IList<Category> Categories { get; set; }
        public IList<Tag> Tags { get; set; }
    }

    public class PostSlugSegment
    {
        public string Title { get; set; }
        public DateTime PubDateUtc { get; set; }
        public DateTime? LastModifyOnUtc { get; set; }
        public string[] Categories { get; set; }
        public string[] Tags { get; set; }
    }
}