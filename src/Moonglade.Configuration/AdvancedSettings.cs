using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class AdvancedSettings : BlogSettings
    {
        public string DNSPrefetchEndpoint { get; set; }

        public string RobotsTxtContent { get; set; }

        public bool EnablePingBackSend { get; set; }

        public bool EnablePingBackReceive { get; set; }

        public bool EnableOpenGraph { get; set; }

        public bool EnableCDNRedirect { get; set; }

        public bool EnableOpenSearch { get; set; }

        public bool EnableMetaWeblog { get; set; }

        public string CDNEndpoint { get; set; }

        public bool FitImageToDevicePixelRatio { get; set; }
    }
}
