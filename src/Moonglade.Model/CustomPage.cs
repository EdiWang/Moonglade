using System;

namespace Moonglade.Model
{
    public class CustomPage
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string RouteName { get; set; }
        public string RawHtmlContent { get; set; }
        public string CssContent { get; set; }
        public bool HideSidebar { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
