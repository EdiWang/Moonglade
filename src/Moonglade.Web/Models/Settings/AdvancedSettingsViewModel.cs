using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class AdvancedSettingsViewModel
    {
        [Display(Name = "DNS Prefetch Endpoint")]
        [DataType(DataType.Url)]
        [MaxLength(128)]
        public string DNSPrefetchEndpoint { get; set; }

        [Display(Name = "robots.txt")]
        [DataType(DataType.MultilineText)]
        [MaxLength(1024)]
        public string RobotsTxtContent { get; set; }

        [Display(Name = "Enable Pingback Send")]
        public bool EnablePingbackSend { get; set; }

        [Display(Name = "Enable Pingback Receive")]
        public bool EnablePingbackReceive { get; set; }

        [Display(Name = "Enable OpenGraph")]
        public bool EnableOpenGraph { get; set; }

        [Display(Name = "Enable CDN for images")]
        public bool EnableCDNRedirect { get; set; }

        [Display(Name = "Enable MetaWeblog API")]
        public bool EnableMetaWeblog { get; set; }

        [Display(Name = "Enable OpenSearch")]
        public bool EnableOpenSearch { get; set; }

        public string CDNEndpoint { get; set; }

        public string MetaWeblogPassword { get; set; }

        [Display(Name = "Fit Image to Device Pixel Ratio")]
        public bool FitImageToDevicePixelRatio { get; set; }
    }
}
