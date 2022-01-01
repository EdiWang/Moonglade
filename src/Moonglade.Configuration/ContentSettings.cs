using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class ContentSettings : IBlogSettings
{
    [Display(Name = "Comment provider")]
    public CommentProvider CommentProvider { get; set; }

    [Display(Name = "Third party comment html pitch")]
    [MaxLength(1024)]
    public string ThirdPartyCommentHtmlPitch { get; set; }

    [Display(Name = "Enable comments")]
    public bool EnableComments { get; set; }

    [Display(Name = "Comments require review and approval")]
    public bool RequireCommentReview { get; set; }

    [Required]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Blocked words")]
    [MaxLength(2048)]
    public string DisharmonyWords { get; set; }

    [Display(Name = "Enable word filter")]
    public bool EnableWordFilter { get; set; }

    [Display(Name = "Word filter mode")]
    public WordFilterMode WordFilterMode { get; set; }

    [Required]
    [Display(Name = "Post list page size")]
    [Range(1, 100, ErrorMessage = "Page size can only range from 1-100")]
    public int PostListPageSize { get; set; }

    [Required]
    [Display(Name = "How many tags show on sidebar")]
    [Range(1, 50, ErrorMessage = "Tag amount can only range from 1-50")]
    public int HotTagAmount { get; set; }

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

    [Display(Name = "Word count in abstract")]
    public int PostAbstractWords { get; set; }

    public ContentSettings()
    {
        DisharmonyWords = string.Empty;
        EnableComments = true;
        PostListPageSize = 10;
        HotTagAmount = 10;
        PostAbstractWords = 400;
    }
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