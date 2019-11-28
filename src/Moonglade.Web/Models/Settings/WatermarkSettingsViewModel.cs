using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
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
        [MaxLength(32)]
        public string WatermarkText { get; set; }
    }
}
