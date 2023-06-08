using Moonglade.Core.CategoryFeature;
using Moonglade.Core.TagFeature;
using System.Linq.Expressions;

namespace Moonglade.Core.PostFeature;

public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Author { get; set; }
    public string RawPostContent { get; set; }
    public bool CommentEnabled { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string ContentAbstract { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeedIncluded { get; set; }
    public bool Featured { get; set; }
    public string ContentLanguageCode { get; set; }
    public bool IsOriginal { get; set; }
    public string OriginLink { get; set; }
    public string HeroImageUrl { get; set; }
    public string InlineCss { get; set; }
    public bool IsOutdated { get; set; }
    public Tag[] Tags { get; set; }
    public Category[] Categories { get; set; }
    public DateTime? PubDateUtc { get; set; }
    public DateTime? LastModifiedUtc { get; set; }

    public static readonly Expression<Func<PostEntity, Post>> EntitySelector = p => new()
    {
        Id = p.Id,
        Title = p.Title,
        Slug = p.Slug,
        Author = p.Author,
        RawPostContent = p.PostContent,
        ContentAbstract = p.ContentAbstract,
        CommentEnabled = p.CommentEnabled,
        CreateTimeUtc = p.CreateTimeUtc,
        PubDateUtc = p.PubDateUtc,
        LastModifiedUtc = p.LastModifiedUtc,
        IsPublished = p.IsPublished,
        IsFeedIncluded = p.IsFeedIncluded,
        Featured = p.IsFeatured,
        IsOriginal = p.IsOriginal,
        OriginLink = p.OriginLink,
        HeroImageUrl = p.HeroImageUrl,
        InlineCss = p.InlineCss,
        IsOutdated = p.IsOutdated,
        ContentLanguageCode = p.ContentLanguageCode,
        Tags = p.Tags.Select(pt => new Tag
        {
            Id = pt.Id,
            NormalizedName = pt.NormalizedName,
            DisplayName = pt.DisplayName
        }).ToArray(),
        Categories = p.PostCategory.Select(pc => new Category
        {
            Id = pc.CategoryId,
            DisplayName = pc.Category.DisplayName,
            RouteName = pc.Category.RouteName,
            Note = pc.Category.Note
        }).ToArray()
    };
}