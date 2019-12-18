using System;

namespace Moonglade.Model
{
    public class CreateCustomPageRequest
    {
        public string Title { get; set; }
        public string RouteName { get; set; }
        public string HtmlContent { get; set; }
        public string CssContent { get; set; }
        public bool HideSidebar { get; set; }
    }

    public class EditCustomPageRequest : CreateCustomPageRequest
    {
        public Guid Id { get; }

        public EditCustomPageRequest(Guid id)
        {
            Id = id;
        }
    }
}
