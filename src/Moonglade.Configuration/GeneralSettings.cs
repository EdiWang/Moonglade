using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class GeneralSettings : BlogSettings
    {
        public string SiteTitle { get; set; }

        public string LogoText { get; set; }

        public string MetaKeyword { get; set; }

        public string MetaDescription { get; set; }

        public string CanonicalPrefix { get; set; }

        public string Copyright { get; set; }

        public string SideBarCustomizedHtmlPitch { get; set; }

        public SideBarOption SideBarOption { get; set; }

        public string FooterCustomizedHtmlPitch { get; set; }

        // Use string instead of TimeSpan as workaround for System.Text.Json issue
        // https://github.com/EdiWang/Moonglade/issues/310
        public string TimeZoneUtcOffset { get; set; }

        public string TimeZoneId { get; set; }

        public string ThemeFileName { get; set; }

        public string OwnerName { get; set; }

        public string OwnerEmail { get; set; }

        public string Description { get; set; }

        public string ShortDescription { get; set; }

        public bool AutoDarkLightTheme { get; set; }

        public GeneralSettings()
        {
            ThemeFileName = "word-blue.css";
        }
    }

    public enum SideBarOption
    {
        Right = 0,
        Left = 1,
        Disabled = 2
    }
}
