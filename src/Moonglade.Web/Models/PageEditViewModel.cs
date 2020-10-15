using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class PageEditViewModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        [MaxLength(128)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter a slug.")]
        [RegularExpression(@"[a-z0-9\-]+", ErrorMessage = "Only lower case letters and hyphens are allowed.")]
        [MaxLength(128)]
        public string Slug { get; set; }

        [Required(ErrorMessage = "Please enter the meta description.")]
        [JsonIgnore]
        [DataType(DataType.MultilineText)]
        [MaxLength(256)]
        public string MetaDescription { get; set; }

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

        [Display(Name = "Published")]
        public bool IsPublished { get; set; }

        public PageEditViewModel()
        {
            Id = Guid.Empty;
            HideSidebar = true;
        }
    }
}
