using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Models
{
    public class PostEditModel
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

        [Required]
        public List<CategoryCheckBox> CategoryList { get; set; }

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

        [Required]
        [Display(Name = "Featured")]
        public bool Featured { get; set; }

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
        public string LanguageCode { get; set; }

        [DataType(DataType.MultilineText)]
        [MaxLength(400)]
        public string Abstract { get; set; }

        [Display(Name = "Publish Date")]
        [DataType(DataType.Date)]
        public DateTime? PublishDate { get; set; }

        [Display(Name = "Change Publish Date")]
        public bool ChangePublishDate { get; set; }

        public PostEditModel()
        {
            PostId = Guid.Empty;
            CategoryList = new();
        }
    }

    public class CategoryCheckBox
    {
        public Guid Id { get; set; }
        public string DisplayText { get; set; }
        public bool IsChecked { get; set; }
    }
}
