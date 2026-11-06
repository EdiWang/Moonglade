using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public sealed record Menu
{
    [Required]
    public string Title { get; init; }

    public string Url { get; init; }

    public string Icon { get; init; } = "icon-file-text2";

    public int DisplayOrder { get; init; }

    public bool IsOpenInNewTab { get; init; }

    [Required]
    public ImmutableList<SubMenu> SubMenus { get; init; } = [];
}
