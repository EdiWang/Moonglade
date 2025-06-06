
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
    public SocialLinkSettings SocialLinkSettings { get; set; } = new();
    public SystemManifestSettings SystemManifestSettings { get; set; } = new();

    private readonly List<string> _keysToInit = [];

    public IEnumerable<string> LoadFromConfig(IDictionary<string, string> config)
    {
        var properties = GetType().GetProperties()
            .Where(p => typeof(IBlogSettings).IsAssignableFrom(p.PropertyType));

        foreach (var prop in properties)
        {
            var currentValue = prop.GetValue(this);
            var defaultValueProp = prop.PropertyType.GetProperty("DefaultValue");
            var defaultValue = defaultValueProp?.GetValue(currentValue);

            var assignedValue = AssignValueForConfigItem((IBlogSettings)defaultValue, config, prop.Name, prop.PropertyType);
            prop.SetValue(this, assignedValue);
        }

        return _keysToInit.AsEnumerable();
    }

    private IBlogSettings AssignValueForConfigItem(IBlogSettings defaultValue, IDictionary<string, string> config, string name, Type type)
    {
        if (config.TryGetValue(name, out var value))
        {
            try
            {
                // Assuming you have a FromJson extension method
                var method = typeof(JsonExtensions).GetMethod("FromJson").MakeGenericMethod(type);
                return (IBlogSettings)method.Invoke(null, [value]);
            }
            catch
            {
                // Handle deserialization error if needed
            }
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