using System.ComponentModel.DataAnnotations;

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

    public CustomMenuSettings()
    {
        Menus = Array.Empty<Menu>();
    }
}