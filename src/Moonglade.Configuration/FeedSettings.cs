using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration
{
    public class FeedSettings : IBlogSettings
    {
        [Display(Name = "RSS items")]
        public int RssItemCount { get; set; }

        [Required]
        [Display(Name = "Copyright")]
        [MaxLength(64)]
        public string RssCopyright { get; set; }

        [Required]
        [Display(Name = "Title")]
        [MaxLength(64)]
        public string RssTitle { get; set; }

        [Required]
        [Display(Name = "Author name")]
        [MaxLength(32)]
        public string AuthorName { get; set; }

        [Display(Name = "Use full blog post content instead of abstract")]
        public bool UseFullContent { get; set; }
    }
}