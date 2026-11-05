using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class CustomMenuSettingsJsonModel
{
    [Display(Name = "Enable custom menus")]
    public bool IsEnabled { get; set; }

    [MaxLength(1024)]
    public string MenuJson { get; set; }
}

public class CustomMenuSettings : IBlogSettings<CustomMenuSettings>, IValidatableObject
{
    public bool IsEnabled { get; set; }

    [MaxLength(5)]
    public Menu[] Menus { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Menus is null)
        {
            if (IsEnabled) yield return new("Menus cannot be null when enabled.", [nameof(Menus)]);
            yield break;
        }

        for (var i = 0; i < Menus.Length; i++)
        {
            var path = $"{nameof(Menus)}[{i}]";
            var menu = Menus[i];
            foreach (var error in ValidateEntry(menu, path)) yield return error;
            if (menu?.SubMenus is null) continue;
            for (var j = 0; j < menu.SubMenus.Count; j++)
                foreach (var error in ValidateEntry(menu.SubMenus[j], $"{path}.{nameof(Menu.SubMenus)}[{j}]"))
                    yield return error;
        }
    }

    private static IEnumerable<ValidationResult> ValidateEntry(object entry, string path)
    {
        if (entry is null)
        {
            yield return new("Menu entries cannot be null.", [path]);
            yield break;
        }
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(entry, new ValidationContext(entry), errors, true);
        foreach (var error in errors)
            yield return new(error.ErrorMessage, error.MemberNames.Select(member => $"{path}.{member}"));
    }

    [JsonIgnore]
    public static CustomMenuSettings DefaultValue =>
        new()
        {
            IsEnabled = true,
            Menus =
            [
                new Menu
                {
                    Title = "About",
                    Url = "/page/about",
                    Icon = "bi-star",
                    DisplayOrder = 1,
                    IsOpenInNewTab = false,
                    SubMenus = []
                }
            ]
        };
}
