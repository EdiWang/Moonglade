using System.Collections.Generic;

namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public int[] WatermarkARGB { get; set; }
        public int WatermarkSkipPixel { get; set; }
        public CaptchaSettings CaptchaSettings { get; set; }
        public int PostAbstractWords { get; set; }
        public Dictionary<string, int> CacheSlidingExpirationMinutes { get; set; }
        public NotificationSettings Notification { get; set; }
        public SiteMapSettings SiteMap { get; set; }

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
        EnableAudit,
        Foaf,
        OPML,
        OpenSearch
    }
}