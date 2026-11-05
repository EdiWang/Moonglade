using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public record SubMenu
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Url { get; set; }

    public bool IsOpenInNewTab { get; set; }
}
