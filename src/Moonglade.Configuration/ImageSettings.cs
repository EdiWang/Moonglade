using Edi.AspNetCore.Utils;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record ImageSettings : IBlogSettings<ImageSettings>, IValidatableObject
{
    [Display(Name = "Enabled watermark")]
    public bool IsWatermarkEnabled { get; init; }

    [Display(Name = "Keep origin image")]
    public bool KeepOriginImage { get; init; }

    [Required]
    [Display(Name = "Font size")]
    [Range(8, 32)]
    public int WatermarkFontSize { get; init; }

    [Required]
    [Display(Name = "Watermark text")]
    [MaxLength(32)]
    public string WatermarkText { get; init; }

    [Required]
    [Display(Name = "A")]
    [Range(0, 255)]
    public int WatermarkColorA { get; init; } = 128;

    [Required]
    [Display(Name = "Watermark skip pixel threshold")]
    [Range(0, int.MaxValue)]
    public int WatermarkSkipPixel { get; init; } = 40000;

    [Display(Name = "Enable CDN for images")]
    public bool EnableCDNRedirect { get; init; }

    [DataType(DataType.Url)]
    [MaxLength(128)]
    [Display(Name = "CDN endpoint")]
    public string CDNEndpoint { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnableCDNRedirect)
        {
            if (string.IsNullOrWhiteSpace(CDNEndpoint))
            {
                yield return new($"{nameof(CDNEndpoint)} must be specified when {nameof(EnableCDNRedirect)} is enabled.", [nameof(CDNEndpoint)]);
            }

            // Validate endpoint Url to avoid security risks
            // But it still has risks:
            // e.g. If the endpoint is compromised, the attacker could return any kind of response from a image with a big fuck to a script that can attack users.

            var endpoint = CDNEndpoint;
            var isValidEndpoint = endpoint.IsValidUrl(UrlExtension.UrlScheme.Https);
            if (!isValidEndpoint)
            {
                yield return new("CDN Endpoint is not a valid HTTPS Url.", [nameof(CDNEndpoint)]);
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
            WatermarkText = "Moonglade"
        };
}
