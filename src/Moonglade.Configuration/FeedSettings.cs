using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record FeedSettings : IBlogSettings<FeedSettings>
{
    [Display(Name = "Feed items")]
    [Range(0, int.MaxValue)]
    public int FeedItemCount { get; init; }

    [Display(Name = "Use full blog post content instead of abstract")]
    public bool UseFullContent { get; init; }

    [JsonIgnore]
    public static FeedSettings DefaultValue => new()
    {
        FeedItemCount = 20,
        UseFullContent = false
    };
}
