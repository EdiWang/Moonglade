using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Widgets;

public class EditWidgetRequest
{
    [Required]
    [Display(Name = "Title")]
    [MaxLength(128)]
    public string Title { get; set; }

    [Required]
    [Display(Name = "Widget Type")]
    public WidgetType WidgetType { get; set; }

    [MaxLength(2000)]
    public string ContentCode { get; set; }

    [Display(Name = "Display Order")]
    [Range(-30, 999)]
    public int DisplayOrder { get; set; }

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;
}
