using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class AdvancedSettingsViewModel
    {
        [Display(Name = "DNS Prefetch Endpoint")]
        [DataType(DataType.Url)]
        [MaxLength(128)]
        public string DNSPrefetchEndpoint { get; set; }

        [Display(Name = "Enable PingBack Send")]
        public bool EnablePingBackSend { get; set; }

        [Display(Name = "Enable PingBack Receive")]
        public bool EnablePingBackReceive { get; set; }
    }
}
