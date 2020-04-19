using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Model;

namespace Moonglade.Web.Models.Settings
{
    public class NavMenuManageViewModel
    {
        public NavMenuEditViewModel NavMenuEditViewModel { get; set; }

        public IReadOnlyList<MenuModel> MenuItems { get; set; }

        public NavMenuManageViewModel()
        {
            NavMenuEditViewModel = new NavMenuEditViewModel();
            MenuItems = new List<MenuModel>();
        }
    }

    public class NavMenuEditViewModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Display(Name = "Title")]
        [MaxLength(64)]
        public string Title { get; set; }

        [Display(Name = "Url (Relative or Absolute)")]
        [MaxLength(256)]
        public string Url { get; set; }

        [Display(Name = "Icon CSS Class")]
        [RegularExpression("[a-z0-9-]+", ErrorMessage = "Invalid Icon CSS Class")]
        public string Icon { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Open in New Tab")]
        public bool IsOpenInNewTab { get; set; }

        public NavMenuEditViewModel()
        {
            Icon = "icon-file-text2";
        }
    }
}
