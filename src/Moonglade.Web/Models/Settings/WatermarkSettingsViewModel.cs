using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class WatermarkSettingsViewModel
    {
        [Display(Name = "Enabled Watermark")]
        public bool IsEnabled { get; set; }

        [Display(Name = "Keep Origin Image")]
        public bool KeepOriginImage { get; set; }

        [Display(Name = "Font Size")]
        [Range(8, 32)]
        public int FontSize { get; set; }

        [Required(ErrorMessage = "Please enter watermark text")]
        [Display(Name = "Watermark Text")]
        [MaxLength(32)]
        public string WatermarkText { get; set; }
    }
}
