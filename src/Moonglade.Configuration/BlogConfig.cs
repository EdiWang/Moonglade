
using System.Text.Json;

namespace Moonglade.Configuration;

public interface IBlogSettings<TSelf> where TSelf : IBlogSettings<TSelf>
{
    static abstract TSelf DefaultValue { get; }
}

public interface IBlogConfig
{
    GeneralSettings GeneralSettings { get; set; }
    ContentSettings ContentSettings { get; set; }
    CommentSettings CommentSettings { get; set; }
    NotificationSettings NotificationSettings { get; set; }
    FeedSettings FeedSettings { get; set; }
    ImageSettings ImageSettings { get; set; }
    AdvancedSettings AdvancedSettings { get; set; }
    AppearanceSettings AppearanceSettings { get; set; }
    CustomMenuSettings CustomMenuSettings { get; set; }
    LocalAccountSettings LocalAccountSettings { get; set; }
    SystemManifestSettings SystemManifestSettings { get; set; }

    IReadOnlyDictionary<string, string> LoadFromConfig(IDictionary<string, string> config, bool isNew = false);
}

public class BlogConfig : IBlogConfig
{
    public GeneralSettings GeneralSettings { get; set; } = new();
    public ContentSettings ContentSettings { get; set; } = new();
    public CommentSettings CommentSettings { get; set; } = new();
    public NotificationSettings NotificationSettings { get; set; } = new();
    public FeedSettings FeedSettings { get; set; } = new();
    public ImageSettings ImageSettings { get; set; } = new();
    public AdvancedSettings AdvancedSettings { get; set; } = new();
    public AppearanceSettings AppearanceSettings { get; set; } = new();
    public CustomMenuSettings CustomMenuSettings { get; set; } = new();
    public LocalAccountSettings LocalAccountSettings { get; set; } = new();
    public SystemManifestSettings SystemManifestSettings { get; set; } = new();

    public IReadOnlyDictionary<string, string> LoadFromConfig(IDictionary<string, string> config, bool isNew = false)
    {
        var defaultsToInit = new Dictionary<string, string>();

        Assign<GeneralSettings>(v => GeneralSettings = v);
        Assign<ContentSettings>(v => ContentSettings = v);
        Assign<CommentSettings>(v => CommentSettings = v);
        Assign<NotificationSettings>(v => NotificationSettings = v);
        Assign<FeedSettings>(v => FeedSettings = v);
        Assign<ImageSettings>(v => ImageSettings = v);
        Assign<AdvancedSettings>(v => AdvancedSettings = v);
        Assign<AppearanceSettings>(v => AppearanceSettings = v);
        Assign<CustomMenuSettings>(v => CustomMenuSettings = v);
        Assign<LocalAccountSettings>(v => LocalAccountSettings = v);
        Assign<SystemManifestSettings>(v => SystemManifestSettings = v,
            () => isNew ? SystemManifestSettings.DefaultValueNew : SystemManifestSettings.DefaultValue);

        return defaultsToInit;

        void Assign<T>(Action<T> setter, Func<T> defaultProvider = null) where T : IBlogSettings<T>
        {
            var name = typeof(T).Name;

            if (config.TryGetValue(name, out var json))
            {
                try
                {
                    var settings = json is null ? default : json.FromJson<T>();
                    if (settings is not null)
                    {
                        setter(settings);
                        return;
                    }
                }
                catch (JsonException)
                {
                    // Fall through to use default value
                }

                if (typeof(T) == typeof(LocalAccountSettings) || typeof(T) == typeof(SystemManifestSettings))
                {
                    throw new InvalidOperationException(
                        $"Stored configuration '{name}' is invalid. Restore a valid value before restarting.");
                }
            }

            var defaultValue = defaultProvider is null ? T.DefaultValue : defaultProvider();
            defaultsToInit.Add(name, defaultValue.ToJson());
            setter(defaultValue);
        }
    }
}
