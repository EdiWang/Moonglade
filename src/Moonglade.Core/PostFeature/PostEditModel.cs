﻿using System.ComponentModel.DataAnnotations;

namespace Moonglade.Core.PostFeature;

public class PostEditModel
{
    [HiddenInput]
    public Guid PostId { get; set; } = Guid.Empty;

    [Required]
    [MaxLength(128)]
    public string Title { get; set; }

    [Required]
    [RegularExpression(@"[a-z0-9\-]+")]
    [MaxLength(128)]
    public string Slug { get; set; }

    [Display(Name = "Author")]
    [MaxLength(64)]
    public string Author { get; set; }

    public Guid[] SelectedCatIds { get; set; }

    [Required]
    [Display(Name = "Enable Comment")]
    public bool EnableComment { get; set; }

    [Required]
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

    [Required]
    [RegularExpression("^[a-z]{2}-[a-zA-Z]{2,4}$")]
    public string LanguageCode { get; set; }

    [DataType(DataType.MultilineText)]
    [MaxLength(400)]
    public string Abstract { get; set; }

    [Display(Name = "Publish Date")]
    [DataType(DataType.Date)]
    public DateTime? PublishDate { get; set; }

    [Display(Name = "Change Publish Date")]
    public bool ChangePublishDate { get; set; }

    [Display(Name = "Origin Link")]
    [DataType(DataType.Url)]
    public string OriginLink { get; set; }

    [Display(Name = "Hero Image")]
    [DataType(DataType.Url)]
    public string HeroImageUrl { get; set; }

    [Display(Name = "Mark as outdated")]
    public bool IsOutdated { get; set; }

    public bool WarnSlugModification => PublishDate.HasValue && (DateTime.UtcNow - PublishDate.Value).Days > 7;
}

