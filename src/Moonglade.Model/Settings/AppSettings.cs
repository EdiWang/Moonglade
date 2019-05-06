namespace Moonglade.Model.Settings
{
    public class AppSettings
    {
        public CaptchaSettings CaptchaSettings { get; set; }
        public bool EnableImageLazyLoad { get; set; }
        public bool EnablePingBackReceive { get; set; }
        public bool UsePictureInsteadOfNotFoundResult { get; set; }
        public int HotTagAmount { get; set; }
        public int PostListPageSize { get; set; }
        public int TimeZone { get; set; }
        public int PostSummaryWords { get; set; }
        public bool EnablePingBackSend { get; set; }
        public bool EnableHarmonizor { get; set; }
        public int ImageCacheSlidingExpirationMinutes { get; set; }
        public bool DisableEmailSendingInDevelopment { get; set; }
    }
}