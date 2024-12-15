using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class AppearanceSettings : IBlogSettings
{
    public int ThemeId { get; set; } = 100;

    [Display(Name = "Enable Custom CSS")]
    public bool EnableCustomCss { get; set; }

    [MaxLength(10240)]
    public string CssCode { get; set; }

    [JsonIgnore]
    public static AppearanceSettings DefaultValue =>
        new()
        {
            ThemeId = 100,
            EnableCustomCss = false,
            CssCode = string.Empty
        };
}