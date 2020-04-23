using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class ContentSettingsViewModel
    {
        [Required]
        [Display(Name = "Enable Comments")]
        public bool EnableComments { get; set; }

        [Required]
        [Display(Name = "Comments Require Blog Admin Review and Approval")]
        public bool RequireCommentReview { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Blocked Words")]
        [MaxLength(2048)]
        public string DisharmonyWords { get; set; }

        [Required]
        [Display(Name = "Enable Word Filter")]
        public bool EnableWordFilter { get; set; }

        [Required]
        [Display(Name = "Use Friendly 404 Image")]
        public bool UseFriendlyNotFoundImage { get; set; }

        [Required]
        [Display(Name = "Post List Page Size")]
        [Range(1, 100, ErrorMessage = "Page Size can only range from 1-1024")]
        public int PostListPageSize { get; set; }

        [Required]
        [Display(Name = "How many tags show on sidebar")]
        [Range(1, 50, ErrorMessage = "Page Size can only range from 1-50")]
        public int HotTagAmount { get; set; }

        [Required]
        [Display(Name = "Enable Gravatar in Comment List")]
        public bool EnableGravatar { get; set; }

        [Display(Name = "Call-out Section Html Pitch")]
        [DataType(DataType.MultilineText)]
        [MaxLength(2048)]
        public string CalloutSectionHtmlPitch { get; set; }

        [Display(Name = "Show Call-out Section")]
        public bool ShowCalloutSection { get; set; }

        [Display(Name = "Show customize footer on each post")]
        public bool ShowPostFooter { get; set; }

        [Display(Name = "Post footer HTML Pitch")]
        public string PostFooterHtmlPitch { get; set; }
    }
}
