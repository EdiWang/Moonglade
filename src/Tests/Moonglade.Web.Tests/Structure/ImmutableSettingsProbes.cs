using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Tests.Structure;

// Test-only candidates; published application models and request contracts are unchanged.
public sealed record ImmutableFeedProbe
{
    [Display(Name = "Feed items")]
    [Range(0, int.MaxValue)]
    public int FeedItemCount { get; init; }

    [Display(Name = "Use full blog post content instead of abstract")]
    public bool UseFullContent { get; init; }
}

public sealed record ImmutableMenusProbe : IValidatableObject
{
    public bool IsEnabled { get; init; }

    [MaxLength(5)]
    public ImmutableList<ImmutableMenuProbe?>? Menus { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Menus is null)
        {
            if (IsEnabled) yield return new("Menus cannot be null when enabled.", [nameof(Menus)]);
            yield break;
        }
        for (var i = 0; i < Menus.Count; i++)
        {
            var menu = Menus[i];
            var path = $"{nameof(Menus)}[{i}]";
            foreach (var error in ValidateEntry(menu, path)) yield return error;
            if (menu?.SubMenus is null) continue;
            for (var j = 0; j < menu.SubMenus.Count; j++)
                foreach (var error in ValidateEntry(menu.SubMenus[j], $"{path}.{nameof(ImmutableMenuProbe.SubMenus)}[{j}]"))
                    yield return error;
        }
    }

    private static IEnumerable<ValidationResult> ValidateEntry(object? value, string path)
    {
        if (value is null)
        {
            yield return new("Menu entries cannot be null.", [path]);
            yield break;
        }
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), errors, true);
        foreach (var error in errors)
            yield return new(error.ErrorMessage, error.MemberNames.Select(member => $"{path}.{member}"));
    }
}

public sealed record ImmutableMenuProbe
{
    [Required]
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Icon { get; init; } = "icon-file-text2";
    public int DisplayOrder { get; init; }
    public bool IsOpenInNewTab { get; init; }
    [Required]
    public ImmutableList<ImmutableSubMenuProbe?>? SubMenus { get; init; } = [];
}

public sealed record ImmutableSubMenuProbe
{
    [Required]
    public string? Title { get; init; }
    [Required]
    public string? Url { get; init; }
    public bool IsOpenInNewTab { get; init; }
}

public sealed record SettingsSnapshotProbe(ImmutableFeedProbe Feed, ImmutableMenusProbe Menu);
