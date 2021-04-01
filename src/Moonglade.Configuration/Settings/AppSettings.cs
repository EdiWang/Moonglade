using System.Collections.Generic;

namespace Moonglade.Configuration.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public int[] WatermarkARGB { get; set; }
        public int WatermarkSkipPixel { get; set; }
        public int PostAbstractWords { get; set; }
        public Dictionary<string, int> CacheSlidingExpirationMinutes { get; set; }
        public NotificationSettings Notification { get; set; }
        public SiteMapSettings SiteMap { get; set; }

        public AppSettings()
        {
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
        MetaWeblog,
        RSD,
        EnableWebApi,
        EnableAudit,
        Foaf,
        OPML
    }
}