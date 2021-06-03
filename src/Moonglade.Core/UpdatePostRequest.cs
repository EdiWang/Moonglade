using System;

namespace Moonglade.Core
{
    public class UpdatePostRequest
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string EditorContent { get; set; }
        public bool EnableComment { get; set; }
        public bool IsPublished { get; set; }
        public bool ExposedToSiteMap { get; set; }
        public bool IsFeedIncluded { get; set; }
        public bool IsFeatured { get; set; }
        public string ContentLanguageCode { get; set; }
        public string Abstract { get; set; }

        public string[] Tags { get; set; }
        public Guid[] CategoryIds { get; set; }

        public DateTime? PublishDate { get; set; }
        public bool IsOriginal { get; set; }
        public string OriginLink { get; set; }

        public UpdatePostRequest()
        {
            Tags = Array.Empty<string>();
            CategoryIds = Array.Empty<Guid>();
        }
    }
}
