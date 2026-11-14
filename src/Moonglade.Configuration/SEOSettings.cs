using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record SEOSettings : IBlogSettings<SEOSettings>
{
    [Display(Name = "Canonical URL prefix")]
    [DataType(DataType.Url)]
    [MaxLength(64)]
    public string CanonicalPrefix { get; init; }

    [Required]
    [Display(Name = "Meta keyword")]
    [MaxLength(256)]
    public string MetaKeyword { get; init; }

    [Display(Name = "Enable Site Map")]
    public bool EnableSiteMap { get; init; } = true;

    [Display(Name = "Enable JSON-LD BlogPosting")]
    public bool EnableJsonLdBlogPosting { get; init; } = true;

    [Display(Name = "Use Dublin Core Metadata")]
    public bool UseDublinCoreMetaData { get; init; }

    [Display(Name = "Dublin Core License URL")]
    public string DcLicenseUrl { get; init; }

    [Display(Name = "robots.txt")]
    [DataType(DataType.MultilineText)]
    [MaxLength(1024)]
    public string RobotsTxtContent { get; init; }

    [JsonIgnore]
    public static SEOSettings DefaultValue => new() { MetaKeyword = "moonglade" };
}
