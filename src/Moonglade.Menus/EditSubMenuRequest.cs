using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Menus;

public class EditSubMenuRequest
{
    [HiddenInput]
    public Guid Id { get; set; }

    [Display(Name = "Title")]
    [MaxLength(64)]
    public string Title { get; set; }

    [Display(Name = "Url (Relative or Absolute)")]
    [MaxLength(256)]
    public string Url { get; set; }

    [Display(Name = "Open in New Tab")]
    public bool IsOpenInNewTab { get; set; }
}