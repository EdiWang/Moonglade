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

public class CustomMenuSettings : IBlogSettings
{
    public bool IsEnabled { get; set; }

    [MaxLength(5)]
    public Menu[] Menus { get; set; }

    [JsonIgnore]
    public static CustomMenuSettings DefaultValue
    {
        get
        {
            return new()
            {
                IsEnabled = true,
                Menus = new[]
                {
                    new Menu
                    {
                        Title = "About",
                        Url = "/page/about",
                        Icon = "bi-star",
                        DisplayOrder = 1,
                        IsOpenInNewTab = false,
                        SubMenus = new()
                    }
                }
            };
        }
    }

    public CustomMenuSettings()
    {
        Menus = Array.Empty<Menu>();
    }
}