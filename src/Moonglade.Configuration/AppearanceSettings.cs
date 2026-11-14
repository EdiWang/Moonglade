using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record AppearanceSettings : IBlogSettings<AppearanceSettings>, IValidatableObject
{
    public int ThemeId { get; init; } = 100;

    [Display(Name = "Enable Custom CSS")]
    public bool EnableCustomCss { get; init; }

    [MaxLength(10240)]
    public string CssCode { get; init; }

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
