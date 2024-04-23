using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class AdvancedSettings : IBlogSettings
{
    [Display(Name = "robots.txt")]
    [DataType(DataType.MultilineText)]
    [MaxLength(1024)]
    public string RobotsTxtContent { get; set; }

    [Display(Name = "Foot JavaScript")]
    [DataType(DataType.MultilineText)]
    [MaxLength(1024)]
    public string FootScripts { get; set; }

    [Display(Name = "Enable Pingback")]
    public bool EnablePingback { get; set; } = true;

    [Display(Name = "Enable OpenSearch")]
    public bool EnableOpenSearch { get; set; } = true;

    [Display(Name = "Enable FOAF")]
    public bool EnableFoaf { get; set; } = true;

    [Display(Name = "Enable OPML")]
    public bool EnableOpml { get; set; } = true;

    [Display(Name = "Enable Site Map")]
    public bool EnableSiteMap { get; set; } = true;

    [Display(Name = "Show warning when clicking external links")]
    public bool WarnExternalLink { get; set; }

    [JsonIgnore]
    public static AdvancedSettings DefaultValue => new();
}