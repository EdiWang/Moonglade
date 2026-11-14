namespace Moonglade.Configuration;

public sealed record BlogSettingsSnapshot
{
    public GeneralSettings GeneralSettings { get; init; } = new();
    public ContentSettings ContentSettings { get; init; } = new();
    public CommentSettings CommentSettings { get; init; } = new();
    public NotificationSettings NotificationSettings { get; init; } = new();
    public SEOSettings SEOSettings { get; init; } = new();
    public FeedSettings FeedSettings { get; init; } = new();
    public ImageSettings ImageSettings { get; init; } = new();
    public AdvancedSettings AdvancedSettings { get; init; } = new();
    public AppearanceSettings AppearanceSettings { get; init; } = new();
    public CustomMenuSettings CustomMenuSettings { get; init; } = new();
}
