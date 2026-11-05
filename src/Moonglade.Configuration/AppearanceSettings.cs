using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text;

namespace Moonglade.Configuration;

public class AppearanceSettings : IBlogSettings<AppearanceSettings>, IValidatableObject
{
    public int ThemeId { get; set; } = 100;

    [Display(Name = "Enable Custom CSS")]
    public bool EnableCustomCss { get; set; }

    [MaxLength(10240)]
    public string CssCode { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EnableCustomCss && string.IsNullOrWhiteSpace(CssCode))
            yield return new("CSS Code is required", [nameof(CssCode)]);
        if (CssCode is not null && Encoding.UTF8.GetByteCount(CssCode) > 10240)
            yield return new("CSS code cannot exceed 10240 UTF-8 bytes.", [nameof(CssCode)]);
    }

    [JsonIgnore]
    public static AppearanceSettings DefaultValue =>
        new()
        {
            ThemeId = 100,
            EnableCustomCss = false,
            CssCode = string.Empty
        };
}
