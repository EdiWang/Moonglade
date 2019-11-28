using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class FeedSettingsViewModel
    {
        [Display(Name = "RSS Items")]
        public int RssItemCount { get; set; }

        [Required]
        [Display(Name = "Copyright")]
        [MaxLength(64)]
        public string RssCopyright { get; set; }

        [Required]
        [Display(Name = "Description")]
        [MaxLength(512)]
        public string RssDescription { get; set; }

        [Required]
        [Display(Name = "RSS Generator Name")]
        [MaxLength(64)]
        public string RssGeneratorName { get; set; }

        [Required]
        [Display(Name = "Title")]
        [MaxLength(64)]
        public string RssTitle { get; set; }

        [Required]
        [Display(Name = "Author Name")]
        [MaxLength(32)]
        public string AuthorName { get; set; }

        public FeedSettingsViewModel()
        {
            RssItemCount = 20;
        }
    }
}
