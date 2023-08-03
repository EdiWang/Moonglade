using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class ContentSettings : IBlogSettings
{
    [Display(Name = "Comment provider")]
    public CommentProvider CommentProvider { get; set; }
    
    [Display(Name = "Comments display order")]
    public CommentOrder CommentOrder { get; set; }

    [Display(Name = "Post title alignment")]
    public PostTitleAlignment PostTitleAlignment { get; set; } = PostTitleAlignment.Left;

    [Display(Name = "Third party comment html pitch")]
    [MaxLength(1024)]
    public string ThirdPartyCommentHtmlPitch { get; set; }

    [Display(Name = "Enable comments")]
    public bool EnableComments { get; set; } = true;

    [Display(Name = "Comments require review and approval")]
    public bool RequireCommentReview { get; set; }

    [Display(Name = "Automatically close comments on posts older than x days")]
    [Range(0, 65536)]
    public int CloseCommentAfterDays { get; set; }

    [DataType(DataType.MultilineText)]
    [Display(Name = "Blocked words")]
    [MaxLength(2048)]
    public string DisharmonyWords { get; set; } = string.Empty;

    [Display(Name = "Enable word filter")]
    public bool EnableWordFilter { get; set; }

    [Display(Name = "Word filter mode")]
    public WordFilterMode WordFilterMode { get; set; }

    [Required]
    [Display(Name = "Post list page size")]
    [Range(5, 30)]
    public int PostListPageSize { get; set; } = 10;

    [Required]
    [Display(Name = "How many tags show on sidebar")]
    [Range(5, 20)]
    public int HotTagAmount { get; set; } = 10;

    [Display(Name = "Enable Gravatar in comment list")]
    public bool EnableGravatar { get; set; }

    [Display(Name = "Call-out section HTML code")]
    [DataType(DataType.MultilineText)]
    [MaxLength(2048)]
    public string CalloutSectionHtmlPitch { get; set; }

    [Display(Name = "Show call-out section")]
    public bool ShowCalloutSection { get; set; }

    [Display(Name = "Show customize footer on each post")]
    public bool ShowPostFooter { get; set; }

    [Display(Name = "Post footer HTML code")]
    public string PostFooterHtmlPitch { get; set; }

    [Display(Name = "Show post outline as side navigation")]
    public bool DocumentOutline { get; set; } = true;

    [Display(Name = "Word count in abstract")]
    public int PostAbstractWords { get; set; } = 400;

    [JsonIgnore]
    public static ContentSettings DefaultValue => new()
    {
        EnableComments = true,
        RequireCommentReview = true,
        EnableGravatar = true,
        EnableWordFilter = false,
        PostListPageSize = 10,
        HotTagAmount = 10,
        DisharmonyWords = "fuck|shit",
        CalloutSectionHtmlPitch = string.Empty
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

public enum PostTitleAlignment
{
    Left = 0,
    Center = 1
}