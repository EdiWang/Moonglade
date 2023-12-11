namespace Moonglade.Configuration;

public interface IBlogSettings
{
}

public interface IBlogConfig
{
	GeneralSettings GeneralSettings { get; set; }
	ContentSettings ContentSettings { get; set; }
	NotificationSettings NotificationSettings { get; set; }
	FeedSettings FeedSettings { get; set; }
	ImageSettings ImageSettings { get; set; }
	AdvancedSettings AdvancedSettings { get; set; }
	CustomStyleSheetSettings CustomStyleSheetSettings { get; set; }
	SocialProfileSettings SocialProfileSettings { get; set; }
	CustomMenuSettings CustomMenuSettings { get; set; }
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

	public SocialProfileSettings SocialProfileSettings { get; set; }

	public CustomMenuSettings CustomMenuSettings { get; set; }

	public IEnumerable<int> LoadFromConfig(IDictionary<string, string> config)
	{
		GeneralSettings = config[nameof(GeneralSettings)].FromJson<GeneralSettings>();
		ContentSettings = config[nameof(ContentSettings)].FromJson<ContentSettings>();
		NotificationSettings = config[nameof(NotificationSettings)].FromJson<NotificationSettings>();
		FeedSettings = config[nameof(FeedSettings)].FromJson<FeedSettings>();
		ImageSettings = config[nameof(ImageSettings)].FromJson<ImageSettings>();
		AdvancedSettings = config[nameof(AdvancedSettings)].FromJson<AdvancedSettings>();
		CustomStyleSheetSettings = config[nameof(CustomStyleSheetSettings)].FromJson<CustomStyleSheetSettings>();
		SocialProfileSettings = config[nameof(SocialProfileSettings)].FromJson<SocialProfileSettings>();

		// Curry code: only migrate new keys added after version 12.9.x
		if (config.ContainsKey(nameof(CustomMenuSettings)))
		{
			CustomMenuSettings = config[nameof(CustomMenuSettings)].FromJson<CustomMenuSettings>();
		}
		else
		{
			CustomMenuSettings = new();
			yield return 10;
		}
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
