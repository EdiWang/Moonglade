using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Moonglade.Web.Models
{
    public class PostEditModel
    {
        [HiddenInput]
        public Guid PostId { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter the slug.")]
        public string Slug { get; set; }

        [JsonIgnore]
        public List<CheckBoxListInfo> CategoryList { get; set; }

        [Required(ErrorMessage = "Please select at least one category.")]
        public Guid[] SelectedCategoryIds { get; set; }

        [Required]
        [Display(Name = "Enable Comment")]
        public bool EnableComment { get; set; }

        [Required(ErrorMessage = "Please enter content.")]
        [JsonIgnore]
        [DataType(DataType.MultilineText)]
        public string HtmlContent { get; set; }

        [Required]
        [Display(Name = "Publish Now")]
        public bool IsPublished { get; set; }

        [Display(Name = "Site Map")]
        public bool ExposedToSiteMap { get; set; }

        [Display(Name = "Feed Subscription")]
        public bool FeedIncluded { get; set; }

        [Display(Name = "Tags")]
        public string Tags { get; set; }

        [Required(ErrorMessage = "Please enter language code.")]
        [Display(Name = "Content Language")]
        public string ContentLanguageCode { get; set; }

        public PostEditModel()
        {
            PostId = Guid.Empty;
            ContentLanguageCode = "en-us";
        }
    }
}
