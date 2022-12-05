using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class FeedSettings : IBlogSettings
{
    [Display(Name = "RSS items")]
    public int RssItemCount { get; set; }

    [Required]
    [Display(Name = "Title")]
    [MaxLength(64)]
    public string RssTitle { get; set; }

    [Display(Name = "Use full blog post content instead of abstract")]
    public bool UseFullContent { get; set; }
}