using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration
{
    public class ImageSettings : IBlogSettings
    {
        [Display(Name = "Enabled watermark")]
        public bool IsWatermarkEnabled { get; set; }

        [Display(Name = "Keep origin image")]
        public bool KeepOriginImage { get; set; }

        [Display(Name = "Font size")]
        [Range(8, 32)]
        public int WatermarkFontSize { get; set; }

        [Required(ErrorMessage = "Please enter watermark text")]
        [Display(Name = "Watermark text")]
        [MaxLength(32)]
        public string WatermarkText { get; set; }

        [Display(Name = "Use friendly 404 image")]
        public bool UseFriendlyNotFoundImage { get; set; }

        [Display(Name = "Fit image to device pixel ratio")]
        public bool FitImageToDevicePixelRatio { get; set; }

        [Display(Name = "Enable CDN for images")]
        public bool EnableCDNRedirect { get; set; }

        [DataType(DataType.Url)]
        [MaxLength(128)]
        [Display(Name = "CDN endpoint")]
        public string CDNEndpoint { get; set; }
    }
}