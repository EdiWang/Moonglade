using Moonglade.Core.TagFeature;
using Moonglade.Data.Entities;
using System.Linq.Expressions;

namespace Moonglade.Core.PostFeature;

public class PostDigest
{
    public DateTime PubDateUtc { get; set; }

    public string Title { get; set; }

    public string Slug { get; set; }

    public string ContentAbstract { get; set; }

    public string LangCode { get; set; }

    public bool IsFeatured { get; set; }

    public IEnumerable<Tag> Tags { get; set; }

    public static Expression<Func<PostEntity, PostDigest>> EntitySelector => p => new()
    {
        Title = p.Title,
        Slug = p.Slug,
        ContentAbstract = p.ContentAbstract,
        PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
        LangCode = p.ContentLanguageCode,
        IsFeatured = p.IsFeatured,
        Tags = p.Tags.Select(pt => new Tag
        {
            NormalizedName = pt.NormalizedName,
            DisplayName = pt.DisplayName
        })
    };

    public static readonly Expression<Func<PostTagEntity, PostDigest>> EntitySelectorByTag = p => new()
    {
        Title = p.Post.Title,
        Slug = p.Post.Slug,
        ContentAbstract = p.Post.ContentAbstract,
        PubDateUtc = p.Post.PubDateUtc.GetValueOrDefault(),
        LangCode = p.Post.ContentLanguageCode,
        IsFeatured = p.Post.IsFeatured,
        Tags = p.Post.Tags.Select(pt => new Tag
        {
            NormalizedName = pt.NormalizedName,
            DisplayName = pt.DisplayName
        })
    };
}