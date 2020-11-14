using System;

namespace Moonglade.Model
{
    public class CreatePostRequest
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string EditorContent { get; set; }
        public bool EnableComment { get; set; }
        public bool IsPublished { get; set; }
        public bool ExposedToSiteMap { get; set; }
        public bool IsFeedIncluded { get; set; }
        public string ContentLanguageCode { get; set; }

        public string[] Tags { get; set; }
        public Guid[] CategoryIds { get; set; }

        public DateTime? PublishDate { get; set; }

        public CreatePostRequest()
        {
            Tags = Array.Empty<string>();
            CategoryIds = Array.Empty<Guid>();
        }
    }

    public class EditPostRequest : CreatePostRequest
    {
        public Guid Id { get; }

        public EditPostRequest(Guid id)
        {
            Id = id;
        }
    }
}
