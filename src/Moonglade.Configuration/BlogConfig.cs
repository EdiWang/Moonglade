
namespace Moonglade.Configuration;

public interface IBlogSettings;

public interface IBlogConfig
{
	GeneralSettings GeneralSettings { get; set; }
	ContentSettings ContentSettings { get; set; }
	NotificationSettings NotificationSettings { get; set; }
	FeedSettings FeedSettings { get; set; }
	ImageSettings ImageSettings { get; set; }
	AdvancedSettings AdvancedSettings { get; set; }
	CustomStyleSheetSettings CustomStyleSheetSettings { get; set; }
	CustomLinkSettings CustomLinkSettings { get; set; }
	CustomMenuSettings CustomMenuSettings { get; set; }
	LocalAccountSettings LocalAccountSettings { get; set; }
	SystemManifestSettings SystemManifestSettings { get; set; }
	IEnumerable<int> LoadFromConfig(IDictionary<string, string> config);
	KeyValuePair<string, string> UpdateAsync<T>(T blogSettings) where T : IBlogSettings;
}

public class BlogConfig : IBlogConfig
{
	public GeneralSettings GeneralSettings { get; set; }

	public ContentSettings ContentSettings { get; set; }

	public NotificationSettings NotificationSettings { get; set; }

	public FeedSettings FeedSettings { get; set; }

	public ImageSettings ImageSettings { get; set; }

	public AdvancedSettings AdvancedSettings { get; set; }

	public CustomStyleSheetSettings CustomStyleSheetSettings { get; set; }
	public CustomLinkSettings CustomLinkSettings { get; set; }

	public CustomMenuSettings CustomMenuSettings { get; set; }

	public LocalAccountSettings LocalAccountSettings { get; set; }

	public SystemManifestSettings SystemManifestSettings { get; set; }

	public IEnumerable<int> LoadFromConfig(IDictionary<string, string> config)
	{
		ContentSettings = AssignValueForConfigItem(1, ContentSettings.DefaultValue, config);
		NotificationSettings = AssignValueForConfigItem(2, NotificationSettings.DefaultValue, config);
		FeedSettings = AssignValueForConfigItem(3, FeedSettings.DefaultValue, config);
		GeneralSettings = AssignValueForConfigItem(4, GeneralSettings.DefaultValue, config);
		ImageSettings = AssignValueForConfigItem(5, ImageSettings.DefaultValue, config);
		AdvancedSettings = AssignValueForConfigItem(6, AdvancedSettings.DefaultValue, config);
		CustomStyleSheetSettings = AssignValueForConfigItem(7, CustomStyleSheetSettings.DefaultValue, config);
		CustomMenuSettings = AssignValueForConfigItem(10, CustomMenuSettings.DefaultValue, config);
		LocalAccountSettings = AssignValueForConfigItem(11, LocalAccountSettings.DefaultValue, config);

		// Special case
		SystemManifestSettings = AssignValueForConfigItem(99, SystemManifestSettings.DefaultValue, config);

		return _keysToInit.AsEnumerable();
	}

	private readonly List<int> _keysToInit = [];
	private T AssignValueForConfigItem<T>(int index, T defaultValue, IDictionary<string, string> config) where T : IBlogSettings
	{
		var name = typeof(T).Name;

		if (config.TryGetValue(name, out var value))
		{
			return value.FromJson<T>();
		}

		_keysToInit.Add(index);
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
