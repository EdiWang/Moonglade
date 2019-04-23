using System;
using System.Collections.Generic;

namespace Moonglade.Model
{
    public class CreateEditPostRequest
    {
        public Guid PostId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string HtmlContent { get; set; }
        public bool EnableComment { get; set; }
        public bool IsPublished { get; set; }
        public bool ExposedToSiteMap { get; set; }
        public bool IsFeedIncluded { get; set; }
        public string ContentLanguageCode { get; set; }

        public IList<string> Tags { get; set; }
        public IList<Guid> CategoryIds { get; set; }

        public CreateEditPostRequest()
        {
            Tags = new List<string>();
            CategoryIds = new List<Guid>();
        }
    }
}
