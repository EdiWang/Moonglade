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
    }
}
