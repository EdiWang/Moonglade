using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record GeneralSettings : IBlogSettings<GeneralSettings>
{
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

    [Required]
    [RegularExpression("^[a-z]{2}-[a-zA-Z]{2,4}$")]
    public string DefaultLanguageCode { get; init; } = "en-us";

    [JsonIgnore]
    public static GeneralSettings DefaultValue => new()
    {
        SiteTitle = "Moonglade",
        LogoText = "moonglade",
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
