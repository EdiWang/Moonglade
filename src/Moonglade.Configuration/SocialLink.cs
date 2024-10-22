using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class SocialLinkSettings : IBlogSettings
{
    public bool IsEnabled { get; set; }

    public List<SocialLink> Links { get; set; }

    public static SocialLinkSettings DefaultValue =>
        new()
        {
            IsEnabled = false,
            Links = new()
        };
}

public class SocialLink
{
    public string Name { get; set; }

    public string Icon { get; set; }

    public string Url { get; set; }
}

public class SocialLinkSettingsJsonModel
{
    public bool IsEnabled { get; set; }

    [MaxLength(1024)]
    public string JsonData { get; set; }
}