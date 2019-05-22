using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Model
{
    public class CreateEditCustomPageRequest
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string RouteName { get; set; }
        public string HtmlContent { get; set; }
        public string CssContent { get; set; }
        public bool HideSidebar { get; set; }
    }
}
