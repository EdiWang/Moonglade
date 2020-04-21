namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public CaptchaSettings CaptchaSettings { get; set; }
        public int PostAbstractWords { get; set; }
        public int ImageCacheSlidingExpirationMinutes { get; set; }
        public bool AllowScriptsInCustomPage { get; set; }
        public bool ShowAdminLoginButton { get; set; }
        public bool EnableAudit { get; set; }
        public bool EnablePostRawEndpoint { get; set; }
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

    public class SystemNavMenus
    {
        public bool Categories { get; set; }

        public bool Tags { get; set; }

        public bool Archive { get; set; }
    }

    public enum EditorChoice
    {
        None = 0,
        HTML = 1,
        Markdown = 2
    }
}