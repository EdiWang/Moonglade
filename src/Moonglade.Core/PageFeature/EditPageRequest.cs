using System.ComponentModel.DataAnnotations;

namespace Moonglade.Core.PageFeature;

public class EditPageRequest
{
    [Required]
    [MaxLength(128)]
    public string Title { get; set; }

    [Required]
    [RegularExpression(@"[a-z0-9\-]+", ErrorMessage = "Only lower case letters and hyphens are allowed.")]
    [MaxLength(128)]
    public string Slug { get; set; }

    [Required]
    [DataType(DataType.MultilineText)]
    [MaxLength(256)]
    public string MetaDescription { get; set; }

    [DataType(DataType.MultilineText)]
    public string RawHtmlContent { get; set; }

    [DataType(DataType.MultilineText)]
    public string CssContent { get; set; }

    [Display(Name = "Hide Sidebar")]
    public bool HideSidebar { get; set; }

    [Display(Name = "Publish")]
    public bool IsPublished { get; set; }

    public EditPageRequest()
    {
        HideSidebar = true;
    }
}