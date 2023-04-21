using Moonglade.Utils;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class ImageSettings : IBlogSettings, IValidatableObject
{
    [Display(Name = "Enabled watermark")]
    public bool IsWatermarkEnabled { get; set; }

    [Display(Name = "Keep origin image")]
    public bool KeepOriginImage { get; set; }

    [Required]
    [Display(Name = "Font size")]
    [Range(8, 32)]
    public int WatermarkFontSize { get; set; }

    [Required]
    [Display(Name = "Watermark text")]
    [MaxLength(32)]
    public string WatermarkText { get; set; }

    [Required]
    [Display(Name = "A")]
    [Range(0, 255)]
    public int WatermarkColorA { get; set; }

    [Required]
    [Display(Name = "R")]
    [Range(0, 255)]
    public int WatermarkColorR { get; set; }

    [Required]
    [Display(Name = "G")]
    [Range(0, 255)]
    public int WatermarkColorG { get; set; }

    [Required]
    [Display(Name = "B")]
    [Range(0, 255)]
    public int WatermarkColorB { get; set; }

    [Required]
    [Display(Name = "Watermark skip pixel threshold")]
    [Range(0, int.MaxValue)]
    public int WatermarkSkipPixel { get; set; }

    [Display(Name = "Fit image to device pixel ratio")]
    public bool FitImageToDevicePixelRatio { get; set; }

    [Display(Name = "Enable CDN for images")]
    public bool EnableCDNRedirect { get; set; }

    [DataType(DataType.Url)]
    [MaxLength(128)]
    [Display(Name = "CDN endpoint")]
    public string CDNEndpoint { get; set; }

    public ImageSettings()
    {
        WatermarkColorA = 128;
        WatermarkColorR = 128;
        WatermarkColorG = 128;
        WatermarkColorB = 128;

        WatermarkSkipPixel = 40000;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnableCDNRedirect)
        {
            if (string.IsNullOrWhiteSpace(CDNEndpoint))
            {
                EnableCDNRedirect = false;
                yield return new($"{nameof(CDNEndpoint)} must be specified when {nameof(EnableCDNRedirect)} is enabled.");
            }

            // Validate endpoint Url to avoid security risks
            // But it still has risks:
            // e.g. If the endpoint is compromised, the attacker could return any kind of response from a image with a big fuck to a script that can attack users.

            var endpoint = CDNEndpoint;
            var isValidEndpoint = endpoint.IsValidUrl(UrlExtension.UrlScheme.Https);
            if (!isValidEndpoint)
            {
                yield return new("CDN Endpoint is not a valid HTTPS Url.");
            }
        }
    }

    [JsonIgnore]
    public static ImageSettings DefaultValue =>
        new()
        {
            IsWatermarkEnabled = true,
            KeepOriginImage = false,
            WatermarkFontSize = 20,
            WatermarkText = "Moonglade",
            FitImageToDevicePixelRatio = true
        };
}