using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class ContentSettings : IBlogSettings
{
    [Required]
    [Display(Name = "Post list page size")]
    [Range(5, 30)]
    public int PostListPageSize { get; set; } = 10;

    [Display(Name = "Post title alignment")]
    public PostTitleAlignment PostTitleAlignment { get; set; } = PostTitleAlignment.Left;

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

    [Display(Name = "Enable view count")]
    public bool EnableViewCount { get; set; } = true;

    [Required]
    [Display(Name = "Maximum page numbers to display in pagination")]
    [Range(2, 10)]
    public int MaximumPageNumbersToDisplay { get; set; } = 5;

    [JsonIgnore]
    public static ContentSettings DefaultValue => new()
    {
        PostListPageSize = 10,
        CalloutSectionHtmlPitch = string.Empty,
        MaximumPageNumbersToDisplay = 5
    };
}

public enum PostTitleAlignment
{
    Left = 0,
    Center = 1
}