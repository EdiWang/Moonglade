using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public sealed record SubMenu
{
    [Required]
    public string Title { get; init; }

    [Required]
    public string Url { get; init; }

    public bool IsOpenInNewTab { get; init; }
}
