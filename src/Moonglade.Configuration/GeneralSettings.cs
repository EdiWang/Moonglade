using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record GeneralSettings : IBlogSettings<GeneralSettings>
{
    [Required]
    [Display(Name = "Meta keyword")]
    [MaxLength(256)]
    public string MetaKeyword { get; init; }

    [Display(Name = "Canonical URL prefix")]
    [DataType(DataType.Url)]
    [MaxLength(64)]
    public string CanonicalPrefix { get; init; }

    [Required]
    [Display(Name = "Logo text")]
    [MaxLength(16)]
    public string LogoText { get; init; }

    [Required]
    [RegularExpression(@"[a-zA-Z0-9\s.\-\[\]]+", ErrorMessage = "Only letters, numbers, - and [] are allowed.")]
    [Display(Name = "Copyright")]
    [MaxLength(64)]
    public string Copyright { get; init; }

    [Required]
    [Display(Name = "Blog title")]
    [MaxLength(16)]
    public string SiteTitle { get; init; }

    [Required]
    [Display(Name = "Your name")]
    [MaxLength(32)]
    public string OwnerName { get; init; }

    [Required]
    [Display(Name = "Owner email")]
    [DataType(DataType.EmailAddress)]
    [MaxLength(64)]
    public string OwnerEmail { get; init; }

    [Required]
    [Display(Name = "Your description")]
    [DataType(DataType.MultilineText)]
    [MaxLength(256)]
    public string Description { get; init; }

    [Display(Name = "Your pronouns")]
    [MaxLength(32)]
    public string Pronouns { get; init; }

    [Display(Name = "Side bar display")]
    [EnumDataType(typeof(SideBarOption))]
    public SideBarOption SideBarOption { get; init; }

    [Display(Name = "Profile")]
    public bool WidgetsProfile { get; init; } = true;

    [Required]
    [Display(Name = "How many tags to show on sidebar")]
    [Range(0, 20)]
    public int HotTagAmount { get; init; } = 10;

    [Display(Name = "Categories")]
    public bool WidgetsCategoryList { get; init; } = true;

    [Display(Name = "Subscription buttons")]
    public bool WidgetsSubscriptionButtons { get; init; } = true;

    // Retained for legacy JSON compatibility; avatars use the local asset endpoint.
    public string AvatarUrl { get; init; }

    [Display(Name = "Use my Gravatar as profile picture")]
    public bool UseGravatarAsProfilePicture { get; init; }

    [Required]
    [RegularExpression("^[a-z]{2}-[a-zA-Z]{2,4}$")]
    public string DefaultLanguageCode { get; init; } = "en-us";

    [Display(Name = "Use Dublin Core Metadata")]
    public bool UseDublinCoreMetaData { get; init; }

    [Display(Name = "Dublin Core License URL")]
    public string DcLicenseUrl { get; init; }

    [JsonIgnore]
    public static GeneralSettings DefaultValue => new()
    {
        OwnerName = "Admin",
        OwnerEmail = "admin@edi.wang",
        SiteTitle = "Moonglade",
        Description = "Moonglade Admin",
        LogoText = "moonglade",
        MetaKeyword = "moonglade",
        Copyright = $"[c] {DateTime.UtcNow.Year}",
        HotTagAmount = 10
    };
}

public enum SideBarOption
{
    Right = 0,
    Left = 1,
    Disabled = 2
}
