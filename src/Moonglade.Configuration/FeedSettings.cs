using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class FeedSettings : IBlogSettings
{
    [Display(Name = "RSS items")]
    public int RssItemCount { get; set; }

    [Display(Name = "Use full blog post content instead of abstract")]
    public bool UseFullContent { get; set; }
}