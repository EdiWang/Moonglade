using System.Collections.Generic;

namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public CaptchaSettings CaptchaSettings { get; set; }
        public int PostAbstractWords { get; set; }
        public Dictionary<string, int> CacheSlidingExpirationMinutes { get; set; }
        public string DefaultLangCode { get; set; }
        public bool EnableOpenGraph { get; set; }
        public bool EnableWebApi { get; set; }
        public bool EnableAudit { get; set; }
        public bool AllowExternalScripts { get; set; }
        public SystemNavMenus SystemNavMenus { get; set; }
        public NotificationSettings Notification { get; set; }

        public AppSettings()
        {
            // Prevent Null Reference Exception if user didn't assign config values
            CaptchaSettings = new CaptchaSettings
            {
                ImageHeight = 36,
                ImageWidth = 100
            };
            SystemNavMenus = new SystemNavMenus
            {
                Archive = true,
                Categories = true,
                Tags = true
            };
            Notification = new NotificationSettings();
        }
    }
}