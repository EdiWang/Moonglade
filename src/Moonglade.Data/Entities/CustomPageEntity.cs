using System;

namespace Moonglade.Data.Entities
{
    public class CustomPageEntity
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string RouteName { get; set; }
        public string HtmlContent { get; set; }
        public string CssContent { get; set; }
        public bool HideSidebar { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }
    }
}
