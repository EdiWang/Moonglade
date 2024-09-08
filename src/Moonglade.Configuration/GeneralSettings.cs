using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class GeneralSettings : IBlogSettings
{
    [Required]
    [Display(Name = "Meta keyword")]
    [MaxLength(256)]
    public string MetaKeyword { get; set; }

    [Display(Name = "Canonical URL prefix")]
    [DataType(DataType.Url)]
    [MaxLength(64)]
    public string CanonicalPrefix { get; set; }

    [Required]
    [Display(Name = "Logo text")]
    [MaxLength(16)]
    public string LogoText { get; set; }

    [Required]
    [RegularExpression(@"[a-zA-Z0-9\s.\-\[\]]+", ErrorMessage = "Only letters, numbers, - and [] are allowed.")]
    [Display(Name = "Copyright")]
    [MaxLength(64)]
    public string Copyright { get; set; }

    [Required]
    [Display(Name = "Blog title")]
    [MaxLength(16)]
    public string SiteTitle { get; set; }

    [Required]
    [Display(Name = "Your name")]
    [MaxLength(32)]
    public string OwnerName { get; set; }

    [Required]
    [Display(Name = "Owner email")]
    [DataType(DataType.EmailAddress)]
    [MaxLength(64)]
    public string OwnerEmail { get; set; }

    [Required]
    [Display(Name = "Your description")]
    [DataType(DataType.MultilineText)]
    [MaxLength(256)]
    public string Description { get; set; }

    [Display(Name = "Your pronouns")]
    [MaxLength(32)]
    public string Pronouns { get; set; }

    [Display(Name = "Side bar HTML code")]
    [DataType(DataType.MultilineText)]
    [MaxLength(2048)]
    public string SideBarCustomizedHtmlPitch { get; set; }

    [Display(Name = "Side bar display")]
    public SideBarOption SideBarOption { get; set; }

    [Display(Name = "Footer HTML code")]
    [DataType(DataType.MultilineText)]
    [MaxLength(4096)]
    public string FooterCustomizedHtmlPitch { get; set; }

    public TimeSpan SelectedUtcOffset { get; set; }

    [MaxLength(64)]
    public string TimeZoneId { get; set; }

    public int ThemeId { get; set; } = 1;

    [Display(Name = "Profile")]
    public bool WidgetsProfile { get; set; } = true;

    [Display(Name = "Tags")]
    public bool WidgetsTags { get; set; } = true;

    [Required]
    [Display(Name = "How many tags to show on sidebar")]
    [Range(5, 20)]
    public int HotTagAmount { get; set; } = 10;

    [Display(Name = "Categories")]
    public bool WidgetsCategoryList { get; set; } = true;

    [Display(Name = "Friend links")]
    public bool WidgetsFriendLink { get; set; } = true;

    [Display(Name = "Subscription buttons")]
    public bool WidgetsSubscriptionButtons { get; set; } = true;

    [MaxLength(64)]
    public string AvatarUrl { get; set; }

    public TimeSpan TimeZoneUtcOffset { get; set; }

    [Required]
    [RegularExpression("^[a-z]{2}-[a-zA-Z]{2,4}$")]
    public string DefaultLanguageCode { get; set; } = "en-us";

    [Display(Name = "Use Dublin Core Metadata")]
    public bool UseDublinCoreMetaData { get; set; }

    [Display(Name = "Dublin Core License URL")]
    public string DcLicenseUrl { get; set; }

    [Display(Name = "Github PAT")]
    public string GithubPat { get; set; }

    [JsonIgnore]
    public static GeneralSettings DefaultValue = new()
    {
        OwnerName = "Admin",
        OwnerEmail = "admin@edi.wang",
        SiteTitle = "Moonglade",
        Description = "Moonglade Admin",
        LogoText = "moonglade",
        MetaKeyword = "moonglade",
        Copyright = $"[c] {DateTime.UtcNow.Year}",
        TimeZoneId = "Coordinated Universal Time",
        TimeZoneUtcOffset = TimeSpan.FromHours(0),
        ThemeId = 1,
        HotTagAmount = 10
    };
}

public enum SideBarOption
{
    Right = 0,
    Left = 1,
    Disabled = 2
}
