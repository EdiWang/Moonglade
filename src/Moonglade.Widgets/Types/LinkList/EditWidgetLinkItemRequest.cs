using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets.Types.LinkList;

public class EditWidgetLinkItemRequest : IValidatableObject
{
    [Required]
    public Guid WidgetId { get; set; }

    [Required]
    [Display(Name = "Title")]
    [MaxLength(64)]
    public string Title { get; set; }

    [Required]
    [Display(Name = "URL")]
    [DataType(DataType.Url)]
    [MaxLength(256)]
    public string Url { get; set; }

    [Display(Name = "Icon Name")]
    [MaxLength(32)]
    public string IconName { get; set; }

    [Display(Name = "Open in New Window")]
    public bool OpenInNewWindow { get; set; } = true;

    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Uri.IsWellFormedUriString(Url, UriKind.Absolute))
        {
            yield return new($"{nameof(Url)} is not a valid url.");
        }
    }
}
