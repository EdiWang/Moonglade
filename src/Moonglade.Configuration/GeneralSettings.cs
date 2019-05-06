using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Configuration
{
    public class GeneralSettings : MoongladeSettings
    {
        public string SiteTitle { get; set; }

        public string LogoText { get; set; }

        public string MetaKeyword { get; set; }

        public string Copyright { get; set; }

        public string SideBarCustomizedHtmlPitch { get; set; }
    }
}
