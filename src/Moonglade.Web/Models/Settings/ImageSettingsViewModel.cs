using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class ImageSettingsViewModel
    {
        [Display(Name = "Enabled Watermark")]
        public bool IsWatermarkEnabled { get; set; }

        [Display(Name = "Keep Origin Image")]
        public bool KeepOriginImage { get; set; }

        [Display(Name = "Font Size")]
        [Range(8, 32)]
        public int WatermarkFontSize { get; set; }

        [Required(ErrorMessage = "Please enter watermark text")]
        [Display(Name = "Watermark Text")]
        [MaxLength(32)]
        public string WatermarkText { get; set; }

        [Display(Name = "Use Friendly 404 Image")]
        public bool UseFriendlyNotFoundImage { get; set; }

        [Display(Name = "Fit Image to Device Pixel Ratio")]
        public bool FitImageToDevicePixelRatio { get; set; }

        [Display(Name = "Enable CDN for images")]
        public bool EnableCDNRedirect { get; set; }

        [DataType(DataType.Url)]
        [MaxLength(128)]
        [Display(Name = "CDN Endpoint")]
        public string CDNEndpoint { get; set; }
    }
}
