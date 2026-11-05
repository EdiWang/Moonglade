using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Moonglade.Utils;

namespace Moonglade.Configuration;

public class AdvancedSettings : IBlogSettings<AdvancedSettings>, IValidatableObject
{
    [Display(Name = "robots.txt")]
    [DataType(DataType.MultilineText)]
    [MaxLength(1024)]
    public string RobotsTxtContent { get; set; }

    [Display(Name = "Head JavaScript")]
    [DataType(DataType.MultilineText)]
    [MaxLength(4096)]
    public string HeadScripts { get; set; }

    [Display(Name = "Foot JavaScript")]
    [DataType(DataType.MultilineText)]
    [MaxLength(4096)]
    public string FootScripts { get; set; }

    [Display(Name = "Enable Webmention")]
    public bool EnableWebmention { get; set; } = true;

    [Display(Name = "Enable OpenSearch")]
    public bool EnableOpenSearch { get; set; } = true;

    [Display(Name = "Enable FOAF")]
    public bool EnableFoaf { get; set; } = true;

    [Display(Name = "Enable OPML")]
    public bool EnableOpml { get; set; } = true;

    [Display(Name = "Enable Site Map")]
    public bool EnableSiteMap { get; set; } = true;

    [Display(Name = "Enable Post Scheduler")]
    public bool EnablePostScheduler { get; set; } = true;

    [Display(Name = "Show warning when clicking external links in posts")]
    public bool WarnPostExternalLink { get; set; }

    [Display(Name = "Show warning when clicking external links in comments")]
    public bool WarnCommentExternalLink { get; set; } = true;

    [Display(Name = "Enable JSON-LD BlogPosting")]
    public bool EnableJsonLdBlogPosting { get; set; } = true;

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
