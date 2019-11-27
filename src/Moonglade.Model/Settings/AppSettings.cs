using System;

namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public CaptchaSettings CaptchaSettings { get; set; }
        public int PostSummaryWords { get; set; }
        public int ImageCacheSlidingExpirationMinutes { get; set; }
        public bool AllowScriptsInCustomPage { get; set; }

        public NotificationSettings Notification { get; set; }
    }
}