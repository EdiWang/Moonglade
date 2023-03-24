using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class CustomMenuSettings : IBlogSettings
{
    [Display(Name = "Enable custom menus")]
    public bool IsEnabled { get; set; }

    [MaxLength(10240)]
    public string MenuJson { get; set; }
}