using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record CommentSettings : IBlogSettings<CommentSettings>
{
    [Display(Name = "Comment provider")]
    [EnumDataType(typeof(CommentProvider))]
    public CommentProvider CommentProvider { get; init; }

    [Display(Name = "Comments display order")]
    [EnumDataType(typeof(CommentOrder))]
    public CommentOrder CommentOrder { get; init; }

    [Display(Name = "Third party comment html pitch")]
    [MaxLength(1024)]
    public string ThirdPartyCommentHtmlPitch { get; init; }

    [Display(Name = "Enable comments")]
    public bool EnableComments { get; init; } = true;

    [Display(Name = "Enable Gravatar in comment list")]
    public bool EnableGravatar { get; init; }

    [Display(Name = "Enable Markdown links in comments and replies")]
    public bool EnableMarkdownLinks { get; init; }

    [Display(Name = "Comments require review and approval")]
    public bool RequireCommentReview { get; init; }

    [Display(Name = "Automatically close comments on posts older than x days")]
    [Range(0, 65536)]
    public int CloseCommentAfterDays { get; init; }

    [Display(Name = "Enable word filter")]
    public bool EnableWordFilter { get; init; }

    [Display(Name = "Word filter mode")]
    [EnumDataType(typeof(WordFilterMode))]
    public WordFilterMode WordFilterMode { get; init; }

    [Display(Name = "Word filter keywords")]
    [MaxLength(4096)]
    public string WordFilterKeywords { get; init; } = "fuck|shit";

    [Display(Name = "Show comment section on mobile screens")]
    public bool EnableCommentSectionOnMobile { get; init; }

    [JsonIgnore]
    public static CommentSettings DefaultValue => new()
    {
        EnableGravatar = true,
        EnableComments = true,
        RequireCommentReview = true,
        EnableWordFilter = false,
        WordFilterKeywords = "fuck|shit"
    };
}

public enum WordFilterMode
{
    Mask = 0,
    Block = 1
}

public enum CommentProvider
{
    BuiltIn = 0,
    ThirdParty = 1
}

public enum CommentOrder
{
    OldToNew = 0,
    NewToOld = 1
}
