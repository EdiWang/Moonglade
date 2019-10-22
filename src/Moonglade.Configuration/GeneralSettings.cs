using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class GeneralSettings : MoongladeSettings
    {
        public string SiteTitle { get; set; }

        public string LogoText { get; set; }

        public string MetaKeyword { get; set; }

        public string Copyright { get; set; }

        public string SideBarCustomizedHtmlPitch { get; set; }

        public string FooterCustomizedHtmlPitch { get; set; }
    }
}
