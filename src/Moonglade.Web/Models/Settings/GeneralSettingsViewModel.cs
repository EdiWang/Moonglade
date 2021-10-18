using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class GeneralSettingsViewModel
    {
        [Required(ErrorMessage = "Please enter keyword")]
        [Display(Name = "Meta keyword")]
        [MaxLength(1024)]
        public string MetaKeyword { get; set; }

        [Required(ErrorMessage = "Please enter description")]
        [Display(Name = "Meta description")]
        [MaxLength(1024)]
        public string MetaDescription { get; set; }

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
        public string OwnerDescription { get; set; }

        [Required(ErrorMessage = "Please enter short description")]
        [Display(Name = "Short description")]
        [MaxLength(32)]
        public string OwnerShortDescription { get; set; }

        [Display(Name = "Side bar HTML code")]
        [DataType(DataType.MultilineText)]
        [MaxLength(2048)]
        public string SideBarCustomizedHtmlPitch { get; set; }

        [Display(Name = "Side bar display")]
        public string SideBarOption { get; set; }

        [Display(Name = "Footer HTML code")]
        [DataType(DataType.MultilineText)]
        [MaxLength(4096)]
        public string FooterCustomizedHtmlPitch { get; set; }

        public TimeSpan SelectedUtcOffset { get; set; }

        [MaxLength(64)]
        public string SelectedTimeZoneId { get; set; }

        [Display(Name = "Auto Light / Dark theme regarding client system settings")]
        public bool AutoDarkLightTheme { get; set; }

        public int SelectedThemeId { get; set; }
    }
}
