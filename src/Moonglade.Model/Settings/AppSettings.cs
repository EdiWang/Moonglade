using System.Collections.Generic;

namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public CaptchaSettings CaptchaSettings { get; set; }
        public int PostAbstractWords { get; set; }
        public Dictionary<string, int> CacheSlidingExpirationMinutes { get; set; }
        public bool EnableWebApi { get; set; }
        public bool EnableAudit { get; set; }
        public bool AllowExternalScripts { get; set; }
        public Dictionary<string, bool> SystemNavMenus { get; set; }
        public Dictionary<string, bool> AsideWidgets { get; set; }
        public NotificationSettings Notification { get; set; }
        public SiteMapSettings SiteMap { get; set; }
        public BlogTheme[] Themes { get; set; }
        public ManifestIcon[] ManifestIcons { get; set; }

        public AppSettings()
        {
            // Prevent Null Reference Exception if user didn't assign config values
            CaptchaSettings = new CaptchaSettings
            {
                ImageHeight = 36,
                ImageWidth = 100
            };
            Notification = new NotificationSettings();
            SiteMap = new SiteMapSettings();
            Themes = new BlogTheme[] { };
            ManifestIcons = new ManifestIcon[] { };
        }
    }
}