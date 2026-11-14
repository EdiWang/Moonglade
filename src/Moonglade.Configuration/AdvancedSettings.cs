using Moonglade.Utils;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record AdvancedSettings : IBlogSettings<AdvancedSettings>, IValidatableObject
{
    [Display(Name = "Head JavaScript")]
    [DataType(DataType.MultilineText)]
    [MaxLength(4096)]
    public string HeadScripts { get; init; }

    [Display(Name = "Foot JavaScript")]
    [DataType(DataType.MultilineText)]
    [MaxLength(4096)]
    public string FootScripts { get; init; }

    [Display(Name = "Enable Webmention")]
    public bool EnableWebmention { get; init; } = true;

    [Display(Name = "Enable OpenSearch")]
    public bool EnableOpenSearch { get; init; } = true;

    [Display(Name = "Enable FOAF")]
    public bool EnableFoaf { get; init; } = true;

    [Display(Name = "Enable OPML")]
    public bool EnableOpml { get; init; } = true;

    [Display(Name = "Enable Post Scheduler")]
    public bool EnablePostScheduler { get; init; } = true;

    [Display(Name = "Show warning when clicking external links in posts")]
    public bool WarnPostExternalLink { get; init; }

    [Display(Name = "Show warning when clicking external links in comments")]
    public bool WarnCommentExternalLink { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ScriptTagValidator.IsValidScriptBlock(HeadScripts))
            yield return new("Only <script>...</script> blocks are allowed.", [nameof(HeadScripts)]);
        if (!ScriptTagValidator.IsValidScriptBlock(FootScripts))
            yield return new("Only <script>...</script> blocks are allowed.", [nameof(FootScripts)]);
    }

    [JsonIgnore]
    public static AdvancedSettings DefaultValue => new();
}
