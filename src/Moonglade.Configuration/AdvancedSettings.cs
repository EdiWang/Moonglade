namespace Moonglade.Configuration
{
    public class AdvancedSettings : IBlogSettings
    {
        public string RobotsTxtContent { get; set; }
        public bool EnablePingBackSend { get; set; }
        public bool EnablePingBackReceive { get; set; }
        public bool EnableOpenGraph { get; set; }
        public bool EnableOpenSearch { get; set; }
        public bool EnableMetaWeblog { get; set; }
        public string MetaWeblogPasswordHash { get; set; }
        public bool WarnExternalLink { get; set; }
        public bool AllowScriptsInPage { get; set; }
        public bool ShowAdminLoginButton { get; set; }
    }
}
