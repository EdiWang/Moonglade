
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

    IEnumerable<string> LoadFromConfig(IDictionary<string, string> config);
    KeyValuePair<string, string> UpdateAsync<T>(T blogSettings) where T : IBlogSettings<T>;
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

    private readonly List<string> _keysToInit = [];

    public IEnumerable<string> LoadFromConfig(IDictionary<string, string> config)
    {
        Assign<GeneralSettings>(config, v => GeneralSettings = v);
        Assign<ContentSettings>(config, v => ContentSettings = v);
        Assign<CommentSettings>(config, v => CommentSettings = v);
        Assign<NotificationSettings>(config, v => NotificationSettings = v);
        Assign<FeedSettings>(config, v => FeedSettings = v);
        Assign<ImageSettings>(config, v => ImageSettings = v);
        Assign<AdvancedSettings>(config, v => AdvancedSettings = v);
        Assign<AppearanceSettings>(config, v => AppearanceSettings = v);
        Assign<CustomMenuSettings>(config, v => CustomMenuSettings = v);
        Assign<LocalAccountSettings>(config, v => LocalAccountSettings = v);
        Assign<SystemManifestSettings>(config, v => SystemManifestSettings = v);

        return _keysToInit.AsEnumerable();
    }

    private void Assign<T>(IDictionary<string, string> config, Action<T> setter) where T : IBlogSettings<T>
    {
        var name = typeof(T).Name;

        if (config.TryGetValue(name, out var json))
        {
            try
            {
                setter(json.FromJson<T>());
                return;
            }
            catch
            {
                // Fall through to use default value
            }
        }

        _keysToInit.Add(name);
        setter(T.DefaultValue);
    }

    public KeyValuePair<string, string> UpdateAsync<T>(T blogSettings) where T : IBlogSettings<T>
    {
        var name = typeof(T).Name;
        var json = blogSettings.ToJson();

        // update singleton itself
        var prop = GetType().GetProperty(name);
        prop?.SetValue(this, blogSettings);

        return new(name, json);
    }
}