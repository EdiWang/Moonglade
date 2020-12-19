using System;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class GeneralSettingsViewModel
    {
        [Required(ErrorMessage = "Please enter keyword")]
        [Display(Name = "Meta Keyword")]
        [MaxLength(1024)]
        public string MetaKeyword { get; set; }

        [Required(ErrorMessage = "Please enter description")]
        [Display(Name = "Meta Description")]
        [MaxLength(1024)]
        public string MetaDescription { get; set; }

        [Display(Name = "Canonical URL Prefix")]
        [DataType(DataType.Url)]
        [MaxLength(64)]
        public string CanonicalPrefix { get; set; }

        [Required(ErrorMessage = "Please enter logo text")]
        [Display(Name = "Logo Text")]
        [MaxLength(16)]
        public string LogoText { get; set; }

        [Required(ErrorMessage = "Please enter copyright")]
        [RegularExpression(@"[a-zA-Z0-9\s.\-\[\]]+", ErrorMessage = "Only letters, numbers, - and [] are allowed.")]
        [Display(Name = "Copyright")]
        [MaxLength(64)]
        public string Copyright { get; set; }

        [Required(ErrorMessage = "Please enter blog title")]
        [Display(Name = "Blog Title")]
        [MaxLength(16)]
        public string SiteTitle { get; set; }

        [Required(ErrorMessage = "Please enter your name")]
        [Display(Name = "Your Name")]
        [MaxLength(32)]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Please enter your description")]
        [Display(Name = "Your Description")]
        [DataType(DataType.MultilineText)]
        [MaxLength(256)]
        public string OwnerDescription { get; set; }

        [Required(ErrorMessage = "Please enter short description")]
        [Display(Name = "Short Description")]
        [MaxLength(32)]
        public string OwnerShortDescription { get; set; }

        [Display(Name = "Side Bar Pitch (HTML)")]
        [DataType(DataType.MultilineText)]
        [MaxLength(2048)]
        public string SideBarCustomizedHtmlPitch { get; set; }

        [Display(Name = "Side Bar Display")]
        public string SideBarOption { get; set; }

        [Display(Name = "Footer Pitch (HTML)")]
        [DataType(DataType.MultilineText)]
        [MaxLength(4096)]
        public string FooterCustomizedHtmlPitch { get; set; }

        public TimeSpan SelectedUtcOffset { get; set; }

        [MaxLength(64)]
        public string SelectedTimeZoneId { get; set; }

        [Display(Name = "Auto Light / Dark theme regarding client system settings")]
        public bool AutoDarkLightTheme { get; set; }

        [MaxLength(32)]
        public string SelectedThemeFileName { get; set; }
    }
}
