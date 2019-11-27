using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models.Settings
{
    public class AdvancedSettingsViewModel
    {
        [Display(Name = "DNS Prefetch Endpoint")]
        [DataType(DataType.Url)]
        public string DNSPrefetchEndpoint { get; set; }

        [Display(Name = "Enable PingBack Send")]
        public bool EnablePingBackSend { get; set; }

        [Display(Name = "Enable PingBack Receive")]
        public bool EnablePingBackReceive { get; set; }
    }
}
