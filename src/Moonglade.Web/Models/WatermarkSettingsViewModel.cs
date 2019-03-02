using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class WatermarkSettingsViewModel
    {
        [Required]
        [Display(Name = "Enabled Watermark")]
        public bool IsEnabled { get; set; }

        [Required]
        [Display(Name = "Keep Origin Image")]
        public bool KeepOriginImage { get; set; }

        [Required]
        [Display(Name = "Font Size")]
        public int FontSize { get; set; }

        [Required]
        [Display(Name = "Watermark Text")]
        public string WatermarkText { get; set; }
    }
}
