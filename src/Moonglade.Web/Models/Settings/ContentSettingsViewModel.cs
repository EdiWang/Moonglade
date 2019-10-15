using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class ContentSettingsViewModel
    {
        [Required]
        [Display(Name = "Enable Comments")]
        public bool EnableComments { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Blocked Words")]
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
    }
}
