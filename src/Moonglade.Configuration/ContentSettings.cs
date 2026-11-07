using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed record ContentSettings : IBlogSettings<ContentSettings>
{
    [Display(Name = "Default editor")]
    [EnumDataType(typeof(EditorChoice))]
    public EditorChoice DefaultEditor { get; init; } = EditorChoice.Html;

    [Required]
    [Display(Name = "Post list page size")]
    [Range(5, 30)]
    public int PostListPageSize { get; init; } = 10;

    [Display(Name = "Post title alignment")]
    [EnumDataType(typeof(PostTitleAlignment))]
    public PostTitleAlignment PostTitleAlignment { get; init; } = PostTitleAlignment.Left;

    [Display(Name = "Call-out section HTML code")]
    [DataType(DataType.MultilineText)]
    [MaxLength(2048)]
    public string CalloutSectionHtmlPitch { get; init; }

    [Display(Name = "Show call-out section")]
    public bool ShowCalloutSection { get; init; }

    [Display(Name = "Show customize footer on each post")]
    public bool ShowPostFooter { get; init; }

    [Display(Name = "Post footer HTML code")]
    public string PostFooterHtmlPitch { get; init; }

    [Display(Name = "Show post outline as side navigation")]
    public bool DocumentOutline { get; init; } = true;

    [Display(Name = "Enable view count")]
    public bool EnableViewCount { get; init; } = true;

    [Required]
    [Display(Name = "Maximum page numbers to display in pagination")]
    [Range(2, 10)]
    public int MaximumPageNumbersToDisplay { get; init; } = 5;

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
