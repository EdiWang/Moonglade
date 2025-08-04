using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class CommentSettings : IBlogSettings
{
    [Display(Name = "Comment provider")]
    public CommentProvider CommentProvider { get; set; }

    [Display(Name = "Comments display order")]
    public CommentOrder CommentOrder { get; set; }

    [Display(Name = "Third party comment html pitch")]
    [MaxLength(1024)]
    public string ThirdPartyCommentHtmlPitch { get; set; }

    [Display(Name = "Enable comments")]
    public bool EnableComments { get; set; } = true;

    [Display(Name = "Enable Gravatar in comment list")]
    public bool EnableGravatar { get; set; }

    [Display(Name = "Comments require review and approval")]
    public bool RequireCommentReview { get; set; }

    [Display(Name = "Automatically close comments on posts older than x days")]
    [Range(0, 65536)]
    public int CloseCommentAfterDays { get; set; }

    [Display(Name = "Enable word filter")]
    public bool EnableWordFilter { get; set; }

    [Display(Name = "Word filter mode")]
    public WordFilterMode WordFilterMode { get; set; }

    [Display(Name = "Show comment section on mobile screens")]
    public bool EnableCommentSectionOnMobile { get; set; }

    [JsonIgnore]
    public static CommentSettings DefaultValue => new()
    {
        EnableGravatar = true,
        EnableComments = true,
        RequireCommentReview = true,
        EnableWordFilter = false
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