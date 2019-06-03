using System;
using System.Collections.Generic;
using Moonglade.Model;

namespace Moonglade.Web.Models
{
    public class PostSlugViewModel
    {
        public string Title { get; set; }
        public string Abstract { get; set; }
        public DateTime PubDateUtc { get; set; }
        public DateTime? LastModifyOnUtc { get; set; }
        public string Content { get; set; }
        public int Hits { get; set; }
        public int Likes { get; set; }
        public string PostId { get; set; }
        public bool CommentEnabled { get; set; }
        public int CommentCount { get; set; }
        public bool IsExposedToSiteMap { get; set; }

        public IList<SimpleCategoryInfoViewModel> Categories { get; set; }
        public IList<Tag> Tags { get; set; }
    }
}