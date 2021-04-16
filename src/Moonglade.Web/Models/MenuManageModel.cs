using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class MenuEditViewModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Title { get; set; }

        [MaxLength(256)]
        public string Url { get; set; }

        [RegularExpression("[a-z0-9-]+", ErrorMessage = "Invalid Icon CSS Class")]
        public string Icon { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Open in New Tab")]
        public bool IsOpenInNewTab { get; set; }

        public SubMenuEditViewModel[] SubMenus { get; set; }
    }

    public class SubMenuEditViewModel
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
}
