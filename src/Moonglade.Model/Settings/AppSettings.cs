namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public CaptchaSettings CaptchaSettings { get; set; }
        public bool EnableImageLazyLoad { get; set; }
        public bool EnablePingBackReceive { get; set; }
        public int TimeZone { get; set; }
        public int PostSummaryWords { get; set; }
        public bool EnablePingBackSend { get; set; }
        public int ImageCacheSlidingExpirationMinutes { get; set; }
        public string DNSPrefetchEndpoint { get; set; }
        public bool AllowScriptsInCustomPage { get; set; }

        public NotificationSettings Notification { get; set; }
    }
}