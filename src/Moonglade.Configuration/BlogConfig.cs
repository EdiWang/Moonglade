
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

	public CustomMenuSettings CustomMenuSettings { get; set; }

	public IEnumerable<int> LoadFromConfig(IDictionary<string, string> config)
	{
		if (config.TryGetValue(nameof(ContentSettings), out var contentSettings))
		{
			ContentSettings = contentSettings.FromJson<ContentSettings>();
		}
		else
		{
			ContentSettings = ContentSettings.DefaultValue;
			yield return 1;
		}

		if (config.TryGetValue(nameof(NotificationSettings), out var notiSettings))
		{
			NotificationSettings = notiSettings.FromJson<NotificationSettings>();
		}
		else
		{
			NotificationSettings = NotificationSettings.DefaultValue;
			yield return 2;
		}

		if (config.TryGetValue(nameof(FeedSettings), out var feedSettings))
		{
			FeedSettings = feedSettings.FromJson<FeedSettings>();
		}
		else
		{
			FeedSettings = FeedSettings.DefaultValue;
			yield return 3;
		}

		if (config.TryGetValue(nameof(GeneralSettings), out var generalSettings))
		{
			GeneralSettings = generalSettings.FromJson<GeneralSettings>();
		}
		else
		{
			GeneralSettings = GeneralSettings.DefaultValue;
			yield return 4;
		}

		if (config.TryGetValue(nameof(ImageSettings), out var imageSettings))
		{
			ImageSettings = imageSettings.FromJson<ImageSettings>();
		}
		else
		{
			ImageSettings = ImageSettings.DefaultValue;
			yield return 5;
		}

		if (config.TryGetValue(nameof(AdvancedSettings), out var advancedSettings))
		{
			AdvancedSettings = advancedSettings.FromJson<AdvancedSettings>();
		}
		else
		{
			AdvancedSettings = AdvancedSettings.DefaultValue;
			yield return 6;
		}

		if (config.TryGetValue(nameof(CustomStyleSheetSettings), out var customStyleSheetSettings))
		{
			CustomStyleSheetSettings = customStyleSheetSettings.FromJson<CustomStyleSheetSettings>();
		}
		else
		{
			CustomStyleSheetSettings = CustomStyleSheetSettings.DefaultValue;
			yield return 7;
		}

		if (config.TryGetValue(nameof(CustomMenuSettings), out var customMenuSettings))
		{
			CustomMenuSettings = customMenuSettings.FromJson<CustomMenuSettings>();
		}
		else
		{
			CustomMenuSettings = CustomMenuSettings.DefaultValue;
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