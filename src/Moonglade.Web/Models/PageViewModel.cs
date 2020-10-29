using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class PageViewModel
    {
        public string Title { get; set; }
        public string MetaDescription { get; set; }
        public string RawHtmlContent { get; set; }
        public string CSS { get; set; }
        public bool HideSidebar { get; set; }
    }
}
