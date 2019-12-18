namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public CaptchaSettings CaptchaSettings { get; set; }
        public int PostSummaryWords { get; set; }
        public int ImageCacheSlidingExpirationMinutes { get; set; }
        public bool AllowScriptsInCustomPage { get; set; }

        public NotificationSettings Notification { get; set; }
    }

    public enum EditorChoice
    {
        None = 0,
        HTML = 1,
        Markdown = 2
    }
}