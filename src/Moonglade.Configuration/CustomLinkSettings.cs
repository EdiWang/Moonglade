using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class CustomLinkSettingsJsonModel
{
	[Display(Name = "Enable custom menus")]
	public bool IsEnabled { get; set; }

	[MaxLength(1024)]
	public string LinkJson { get; set; }
}

public class CustomLinkSettings : IBlogSettings
{
	public bool IsEnabled { get; set; }

	[MaxLength(5)]
	public Link[] Links { get; set; } = Array.Empty<Link>();

	[JsonIgnore]
	public static CustomLinkSettings DefaultValue =>
		new()
		{
			IsEnabled = true,
			Links =
			[
				new Link
				{
					Title = "LinkedIn",
					Url = "https://linkedin.com/in/yourUsername",
					Icon = "bi-star",
					IsOpenInNewTab = true
				}
			]
		};
}
