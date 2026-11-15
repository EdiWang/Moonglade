using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record FeedSettings : IBlogSettings<FeedSettings>
{
    [Display(Name = "Enable subscription")]
    public bool EnableSubscription { get; init; } = true;

    [Display(Name = "Feed type")]
    [EnumDataType(typeof(FeedType))]
    public FeedType FeedType { get; init; } = FeedType.Both;

    [Display(Name = "Feed items")]
    [Range(0, int.MaxValue)]
    public int FeedItemCount { get; init; }

    [Display(Name = "Use full blog post content instead of abstract")]
    public bool UseFullContent { get; init; }

    public bool IsFeedEnabled(FeedType type) =>
        EnableSubscription && (FeedType == type || FeedType == FeedType.Both);

    [JsonIgnore]
    public static FeedSettings DefaultValue => new()
    {
        FeedItemCount = 20,
        UseFullContent = false
    };
}

public enum FeedType
{
    Rss = 0,
    Atom = 1,
    Both = 2
}
