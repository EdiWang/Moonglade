using System;

namespace Moonglade.Model
{
    public class CreatePageRequest
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string MetaDescription { get; set; }
        public string HtmlContent { get; set; }
        public string CssContent { get; set; }
        public bool HideSidebar { get; set; }
        public bool IsPublished { get; set; }
    }

    public class EditPageRequest : CreatePageRequest
    {
        public Guid Id { get; }

        public EditPageRequest(Guid id)
        {
            Id = id;
        }
    }
}
