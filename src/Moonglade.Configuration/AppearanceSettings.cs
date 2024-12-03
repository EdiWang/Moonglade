using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class AppearanceSettings : IBlogSettings
{
    [Display(Name = "Enable Custom CSS")]
    public bool EnableCustomCss { get; set; }

    [MaxLength(10240)]
    public string CssCode { get; set; }

    [JsonIgnore]
    public static AppearanceSettings DefaultValue =>
        new()
        {
            EnableCustomCss = false,
            CssCode = string.Empty
        };
}