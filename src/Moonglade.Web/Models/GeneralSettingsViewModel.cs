using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models
{
    public class GeneralSettingsViewModel
    {
        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Blocked Words")]
        public string DisharmonyWords { get; set; }

        [Required]
        [Display(Name = "Meta Keyword")]
        public string MetaKeyword { get; set; }

        [Required]
        [Display(Name = "Logo Text")]
        public string LogoText { get; set; }

        [Required]
        [RegularExpression(@"[a-zA-Z0-9\s.\-\[\]]+", ErrorMessage = "Only letters, numbers, - and [] are allowed.")]
        [Display(Name = "Copyright")]
        public string Copyright { get; set; }

        [Required]
        [Display(Name = "Website Title")]
        public string SiteTitle { get; set; }

        [Required]
        [Display(Name = "Enable Comments")]
        public bool EnableComments { get; set; }

        [Required]
        [Display(Name = "Blogger Name")]
        public string BloggerName { get; set; }

        [Required]
        [Display(Name = "Blogger Description (HTML)")]
        [DataType(DataType.MultilineText)]
        public string BloggerDescription { get; set; }

        [Required]
        [Display(Name = "Blogger Short Description")]
        public string BloggerShortDescription { get; set; }

        [Display(Name = "Side Bar Customized Html Pitch")]
        [DataType(DataType.MultilineText)]
        public string SideBarCustomizedHtmlPitch { get; set; }
    }
}
