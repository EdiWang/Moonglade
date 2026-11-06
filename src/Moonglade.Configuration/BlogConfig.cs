
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public interface IBlogSettings<TSelf> where TSelf : IBlogSettings<TSelf>
{
    static abstract TSelf DefaultValue { get; }
}

public interface IBlogConfig
{
    GeneralSettings GeneralSettings { get; }
    ContentSettings ContentSettings { get; }
    CommentSettings CommentSettings { get; }
    NotificationSettings NotificationSettings { get; }
    FeedSettings FeedSettings { get; }
    ImageSettings ImageSettings { get; }
    AdvancedSettings AdvancedSettings { get; }
    AppearanceSettings AppearanceSettings { get; }
    CustomMenuSettings CustomMenuSettings { get; }

    BlogSettingsSnapshot Capture();
}

public class BlogConfig : IBlogConfig
{
    private BlogSettingsSnapshot _current = new();

    public BlogConfig() { }

    public BlogConfig(BlogSettingsSnapshot snapshot) => Publish(snapshot);

    public BlogSettingsSnapshot Capture() => Volatile.Read(ref _current);

    // shortcut: per-group readers can recapture; migrate related operations/renders in S3.
    public GeneralSettings GeneralSettings => Capture().GeneralSettings;
    public ContentSettings ContentSettings => Capture().ContentSettings;
    public CommentSettings CommentSettings => Capture().CommentSettings;
    public NotificationSettings NotificationSettings => Capture().NotificationSettings;
    public FeedSettings FeedSettings => Capture().FeedSettings;
    public ImageSettings ImageSettings => Capture().ImageSettings;
    public AdvancedSettings AdvancedSettings => Capture().AdvancedSettings;
    public AppearanceSettings AppearanceSettings => Capture().AppearanceSettings;
    public CustomMenuSettings CustomMenuSettings => Capture().CustomMenuSettings;

    public void Publish(BlogSettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Volatile.Write(ref _current, snapshot);
    }

    public IReadOnlyDictionary<string, string> LoadFromConfig(IDictionary<string, string> config, bool isNew = false)
    {
        var defaultsToInit = new Dictionary<string, string>();

        var snapshot = new BlogSettingsSnapshot
        {
            GeneralSettings = Read<GeneralSettings>(),
            ContentSettings = Read<ContentSettings>(),
            CommentSettings = Read<CommentSettings>(),
            NotificationSettings = Read<NotificationSettings>(),
            FeedSettings = Read<FeedSettings>(),
            ImageSettings = Read<ImageSettings>(),
            AdvancedSettings = Read<AdvancedSettings>(),
            AppearanceSettings = Read<AppearanceSettings>(),
            CustomMenuSettings = Read<CustomMenuSettings>(),
        };
        Publish(snapshot);
        return defaultsToInit;

        T Read<T>() where T : IBlogSettings<T>
        {
            var name = typeof(T).Name;

            if (config.TryGetValue(name, out var json))
            {
                try
                {
                    var settings = json is null ? default : json.FromJsonWithDefaults(T.DefaultValue);
                    if (settings is not null && Validator.TryValidateObject(settings, new ValidationContext(settings), null, true))
                    {
                        return settings;
                    }
                }
                catch (JsonException)
                {
                    // Fall through to use default value
                }
            }

            var defaultValue = T.DefaultValue;
            defaultsToInit.Add(name, defaultValue.ToJson());
            return defaultValue;
        }
    }
}
