
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Moonglade.Configuration;

public interface IBlogSettings<TSelf> where TSelf : IBlogSettings<TSelf>
{
    static abstract TSelf DefaultValue { get; }
}

public interface IBlogConfig
{
    BlogSettingsSnapshot Capture();
}

public class BlogConfig : IBlogConfig
{
    private BlogSettingsSnapshot _current = new();

    public BlogConfig() { }

    public BlogConfig(BlogSettingsSnapshot snapshot) => Publish(snapshot);

    public BlogSettingsSnapshot Capture() => Volatile.Read(ref _current);

    public void Publish(BlogSettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Volatile.Write(ref _current, snapshot);
    }

    public static (BlogSettingsSnapshot Snapshot, Dictionary<string, string> Defaults) Parse(IDictionary<string, string> config)
    {
        var defaultsToInit = new Dictionary<string, string>();

        var snapshot = new BlogSettingsSnapshot
        {
            GeneralSettings = Read<GeneralSettings>(),
            ContentSettings = Read<ContentSettings>(),
            CommentSettings = Read<CommentSettings>(),
            NotificationSettings = Read<NotificationSettings>(),
            SEOSettings = Read<SEOSettings>(),
            FeedSettings = Read<FeedSettings>(),
            ImageSettings = Read<ImageSettings>(),
            AdvancedSettings = Read<AdvancedSettings>(),
            AppearanceSettings = Read<AppearanceSettings>(),
            CustomMenuSettings = Read<CustomMenuSettings>(),
        };
        return (snapshot, defaultsToInit);

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
