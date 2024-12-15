
namespace Moonglade.Configuration;

public interface IBlogSettings;

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
    SocialLinkSettings SocialLinkSettings { get; set; }
    SystemManifestSettings SystemManifestSettings { get; set; }

    IEnumerable<string> LoadFromConfig(IDictionary<string, string> config);
    KeyValuePair<string, string> UpdateAsync<T>(T blogSettings) where T : IBlogSettings;
}

public class BlogConfig : IBlogConfig
{
    public GeneralSettings GeneralSettings { get; set; }

    public ContentSettings ContentSettings { get; set; }

    public CommentSettings CommentSettings { get; set; }

    public NotificationSettings NotificationSettings { get; set; }

    public FeedSettings FeedSettings { get; set; }

    public ImageSettings ImageSettings { get; set; }

    public AdvancedSettings AdvancedSettings { get; set; }

    public AppearanceSettings AppearanceSettings { get; set; }

    public CustomMenuSettings CustomMenuSettings { get; set; }

    public LocalAccountSettings LocalAccountSettings { get; set; }

    public SocialLinkSettings SocialLinkSettings { get; set; }

    public SystemManifestSettings SystemManifestSettings { get; set; }

    public IEnumerable<string> LoadFromConfig(IDictionary<string, string> config)
    {
        ContentSettings = AssignValueForConfigItem(ContentSettings.DefaultValue, config);
        NotificationSettings = AssignValueForConfigItem(NotificationSettings.DefaultValue, config);
        FeedSettings = AssignValueForConfigItem(FeedSettings.DefaultValue, config);
        GeneralSettings = AssignValueForConfigItem(GeneralSettings.DefaultValue, config);
        ImageSettings = AssignValueForConfigItem(ImageSettings.DefaultValue, config);
        AdvancedSettings = AssignValueForConfigItem(AdvancedSettings.DefaultValue, config);
        AppearanceSettings = AssignValueForConfigItem(AppearanceSettings.DefaultValue, config);
        CommentSettings = AssignValueForConfigItem(CommentSettings.DefaultValue, config);
        CustomMenuSettings = AssignValueForConfigItem(CustomMenuSettings.DefaultValue, config);
        LocalAccountSettings = AssignValueForConfigItem(LocalAccountSettings.DefaultValue, config);
        SocialLinkSettings = AssignValueForConfigItem(SocialLinkSettings.DefaultValue, config);

        // Special case
        SystemManifestSettings = AssignValueForConfigItem(SystemManifestSettings.DefaultValue, config);

        return _keysToInit.AsEnumerable();
    }

    private readonly List<string> _keysToInit = [];
    private T AssignValueForConfigItem<T>(T defaultValue, IDictionary<string, string> config) where T : IBlogSettings
    {
        var name = typeof(T).Name;

        if (config.TryGetValue(name, out var value))
        {
            return value.FromJson<T>();
        }

        _keysToInit.Add(name);
        return defaultValue;
    }

    public KeyValuePair<string, string> UpdateAsync<T>(T blogSettings) where T : IBlogSettings
    {
        var name = typeof(T).Name;
        var json = blogSettings.ToJson();

        // update singleton itself
        var prop = GetType().GetProperty(name);
        prop?.SetValue(this, blogSettings);

        return new(name, json);
    }
}