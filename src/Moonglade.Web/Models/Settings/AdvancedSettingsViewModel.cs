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
    }
}
