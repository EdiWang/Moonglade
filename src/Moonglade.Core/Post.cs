using System;

namespace Moonglade.Core
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Author { get; set; }
        public string RawPostContent { get; set; }
        public bool CommentEnabled { get; set; }
        public DateTime CreateTimeUtc { get; set; }
        public string ContentAbstract { get; set; }
        public bool IsPublished { get; set; }
        public bool ExposedToSiteMap { get; set; }
        public bool IsFeedIncluded { get; set; }
        public bool Featured { get; set; }
        public string ContentLanguageCode { get; set; }
        public bool IsOriginal { get; set; }
        public string OriginLink { get; set; }
        public string HeroImageUrl { get; set; }
        public Tag[] Tags { get; set; }
        public Category[] Categories { get; set; }
        public DateTime? PubDateUtc { get; set; }
        public DateTime? LastModifiedUtc { get; set; }
    }
}
