using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Core.PostFeature;

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

    [Display(Name = "Author")]
    [MaxLength(64)]
    public string Author { get; set; }

    [Required]
    [MinLength(1)]
    public Guid[] SelectedCatIds { get; set; }

    [Required]
    [Display(Name = "Enable Comment")]
    public bool EnableComment { get; set; }

    [Required(ErrorMessage = "Please enter content.")]
    [DataType(DataType.MultilineText)]
    public string EditorContent { get; set; }

    [Required]
    [Display(Name = "Publish Now")]
    public bool IsPublished { get; set; }

    [Required]
    [Display(Name = "Featured")]
    public bool Featured { get; set; }

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

    [Display(Name = "Original")]
    public bool IsOriginal { get; set; }

    [Display(Name = "Origin Link")]
    [DataType(DataType.Url)]
    public string OriginLink { get; set; }

    [Display(Name = "Hero Image")]
    [DataType(DataType.Url)]
    public string HeroImageUrl { get; set; }

    [Display(Name = "Inline CSS")]
    [MaxLength(2048)]
    public string InlineCss { get; set; }

    public PostEditModel()
    {
        PostId = Guid.Empty;
        IsOriginal = true;
    }
}