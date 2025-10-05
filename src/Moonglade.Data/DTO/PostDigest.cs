using Moonglade.Data.Entities;
using System.Linq.Expressions;

namespace Moonglade.Data.DTO;

public class PostDigest
{
    public DateTime PubDateUtc { get; set; }

    public string Title { get; set; }

    public string Slug { get; set; }

    public string ContentAbstract { get; set; }

    public string LangCode { get; set; }

    public bool IsFeatured { get; set; }

    public IEnumerable<Tag> Tags { get; set; }

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