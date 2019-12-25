using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class PostEditViewModel
    {
        [HiddenInput]
        public Guid PostId { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        [MaxLength(128)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter the slug.")]
        [RegularExpression(@"[a-z0-9\-]+", ErrorMessage = "Only lower case letters and hyphens are allowed.")]
        [MaxLength(128)]
        public string Slug { get; set; }

        [JsonIgnore]
        public List<CheckBoxViewModel> CategoryList { get; set; }

        [Required(ErrorMessage = "Please select at least one category.")]
        public Guid[] SelectedCategoryIds { get; set; }

        [Required]
        [Display(Name = "Enable Comment")]
        public bool EnableComment { get; set; }

        [Required(ErrorMessage = "Please enter content.")]
        [JsonIgnore]
        [DataType(DataType.MultilineText)]
        public string EditorContent { get; set; }

        [Required]
        [Display(Name = "Publish Now")]
        public bool IsPublished { get; set; }

        [Display(Name = "Site Map")]
        public bool ExposedToSiteMap { get; set; }

        [Display(Name = "Feed Subscription")]
        public bool FeedIncluded { get; set; }

        [Display(Name = "Tags")]
        [MaxLength(128)]
        public string Tags { get; set; }

        [Required(ErrorMessage = "Please enter language code.")]
        [Display(Name = "Content Language")]
        [RegularExpression("^[a-z]{2}-[a-zA-Z]{2}$", ErrorMessage = "Incorrect language code format. e.g. en-us")]
        public string ContentLanguageCode { get; set; }

        public PostEditViewModel()
        {
            PostId = Guid.Empty;
            ContentLanguageCode = "en-us";
        }
    }
}
