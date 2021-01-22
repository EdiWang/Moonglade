using System;

namespace Moonglade.Core
{
    public class Page
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string MetaDescription { get; set; }
        public string RawHtmlContent { get; set; }
        public string CssContent { get; set; }
        public bool HideSidebar { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreateTimeUtc { get; set; }
        public DateTime? UpdateTimeUtc { get; set; }
    }
}
