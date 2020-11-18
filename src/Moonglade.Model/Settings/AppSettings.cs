using System;
using System.Collections.Generic;

namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public int[] WatermarkARGB { get; set; }
        public CaptchaSettings CaptchaSettings { get; set; }
        public int PostAbstractWords { get; set; }
        public Dictionary<string, int> CacheSlidingExpirationMinutes { get; set; }
        public Dictionary<string, bool> SystemNavMenus { get; set; }
        public Dictionary<string, bool> AsideWidgets { get; set; }
        public NotificationSettings Notification { get; set; }
        public SiteMapSettings SiteMap { get; set; }
        public BlogTheme[] Themes { get; set; }
        public ManifestIcon[] ManifestIcons { get; set; }
        public TagNormalization[] TagNormalization { get; set; }

        public AppSettings()
        {
            // Prevent Null Reference Exception if user didn't assign config values
            CaptchaSettings = new()
            {
                ImageHeight = 36,
                ImageWidth = 100
            };
            Notification = new();
            SiteMap = new();
            Themes = Array.Empty<BlogTheme>();
            ManifestIcons = Array.Empty<ManifestIcon>();
        }
    }

    public class TagNormalization
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }

    public enum FeatureFlags
    {
        EnableWebApi,
        EnableAudit
    }
}