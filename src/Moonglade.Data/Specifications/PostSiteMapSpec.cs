using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostSiteMapSpec : Specification<PostEntity, PostSiteMapInfo>
{
    public PostSiteMapSpec()
    {
        Query.Where(p => p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);
        Query.Select(p => new PostSiteMapInfo
        {
            RouteLink = p.RouteLink,
            CreateTimeUtc = p.PubDateUtc.GetValueOrDefault(),
            UpdateTimeUtc = p.LastModifiedUtc
        });
        Query.AsNoTracking();
    }
}

public class PostSiteMapInfo
{
    public string RouteLink { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public DateTime? UpdateTimeUtc { get; set; }
}