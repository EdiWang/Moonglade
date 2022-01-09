using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class GeneralSettings : IBlogSettings
{
    [Required(ErrorMessage = "Please enter keyword")]
    [Display(Name = "Meta keyword")]
    [MaxLength(1024)]
    public string MetaKeyword { get; set; }

    [Display(Name = "Canonical URL prefix")]
    [DataType(DataType.Url)]
    [MaxLength(64)]
    public string CanonicalPrefix { get; set; }

    [Required(ErrorMessage = "Please enter logo text")]
    [Display(Name = "Logo text")]
    [MaxLength(16)]
    public string LogoText { get; set; }

    [Required(ErrorMessage = "Please enter copyright")]
    [RegularExpression(@"[a-zA-Z0-9\s.\-\[\]]+", ErrorMessage = "Only letters, numbers, - and [] are allowed.")]
    [Display(Name = "Copyright")]
    [MaxLength(64)]
    public string Copyright { get; set; }

    [Required(ErrorMessage = "Please enter blog title")]
    [Display(Name = "Blog title")]
    [MaxLength(16)]
    public string SiteTitle { get; set; }

    [Required(ErrorMessage = "Please enter your name")]
    [Display(Name = "Your name")]
    [MaxLength(32)]
    public string OwnerName { get; set; }

    [Required]
    [Display(Name = "Owner email")]
    [DataType(DataType.EmailAddress)]
    [MaxLength(64)]
    public string OwnerEmail { get; set; }

    [Required(ErrorMessage = "Please enter your description")]
    [Display(Name = "Your description")]
    [DataType(DataType.MultilineText)]
    [MaxLength(256)]
    public string Description { get; set; }

    [Required(ErrorMessage = "Please enter short description")]
    [Display(Name = "Short description")]
    [MaxLength(32)]
    public string ShortDescription { get; set; }

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

    [Display(Name = "Auto Light / Dark theme regarding client system settings")]
    public bool AutoDarkLightTheme { get; set; }

    public int ThemeId { get; set; }

    [Display(Name = "Show pride mouse cursor and flag")]
    public bool Pride { get; set; }

    /// <summary>
    /// Avatar Url
    /// </summary>
    [MaxLength(64)]
    public string AvatarUrl { get; set; }

    // Use string instead of TimeSpan as workaround for System.Text.Json issue
    // https://github.com/EdiWang/Moonglade/issues/310
    public string TimeZoneUtcOffset { get; set; }

    public GeneralSettings()
    {
        ThemeId = 1;
    }
}

public enum SideBarOption
{
    Right = 0,
    Left = 1,
    Disabled = 2
}