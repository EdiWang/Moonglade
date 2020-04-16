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
        public string Title { get; set; }

        [Display(Name = "Url")]
        [DataType(DataType.Url)]
        public string Url { get; set; }

        [Display(Name = "Icon")]
        // TODO: Regex
        public string Icon { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        public NavMenuEditViewModel()
        {
            Icon = "icon-file-text2";
        }
    }
}
