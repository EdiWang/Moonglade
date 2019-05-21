using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Moonglade.Web.Models
{
    public class CustomPageEditViewModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter the route name.")]
        [RegularExpression(@"[a-z0-9\-]+", ErrorMessage = "Only lower case letters and hyphens are allowed.")]
        public string RouteName { get; set; }

        [Required(ErrorMessage = "Please enter content.")]
        [JsonIgnore]
        [DataType(DataType.MultilineText)]
        public string RawHtmlContent { get; set; }

        [JsonIgnore]
        [DataType(DataType.MultilineText)]
        public string CssContent { get; set; }

        [Required]
        [Display(Name = "Hide Sidebar")]
        public bool HideSidebar { get; set; }

        public CustomPageEditViewModel()
        {
            Id = Guid.Empty;
            HideSidebar = true;
        }
    }
}
