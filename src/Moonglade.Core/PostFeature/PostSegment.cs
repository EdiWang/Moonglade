using Moonglade.Data;
using System.Linq.Expressions;

namespace Moonglade.Core.PostFeature;

public struct PostSegment
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string ContentAbstract { get; set; }
    public DateTime? PubDateUtc { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public string PostStatus { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOutdated { get; set; }

    public static readonly Expression<Func<PostEntity, PostSegment>> EntitySelector = p => new()
    {
        Id = p.Id,
        Title = p.Title,
        Slug = p.Slug,
        PubDateUtc = p.PubDateUtc,
        PostStatus = p.PostStatus,
        IsFeatured = p.IsFeatured,
        IsDeleted = p.IsDeleted,
        IsOutdated = p.IsOutdated,
        CreateTimeUtc = p.CreateTimeUtc,
        LastModifiedUtc = p.LastModifiedUtc,
        ContentAbstract = p.ContentAbstract
    };
}