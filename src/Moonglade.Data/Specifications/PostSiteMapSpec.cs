using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostSiteMapSpec : Specification<PostEntity, PostSiteMapInfo>
{
    public PostSiteMapSpec()
    {
        Query.Where(p => p.IsPublished && !p.IsDeleted);
        Query.Select(p => new PostSiteMapInfo
        {
            Slug = p.Slug,
            CreateTimeUtc = p.PubDateUtc.GetValueOrDefault(),
            UpdateTimeUtc = p.LastModifiedUtc
        });
        Query.AsNoTracking();
    }
}

public class PostSiteMapInfo
{
    public string Slug { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public DateTime? UpdateTimeUtc { get; set; }
}