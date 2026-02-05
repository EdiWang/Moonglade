using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostSiteMapSpec : Specification<PostEntity, SiteMapInfo>
{
    public PostSiteMapSpec()
    {
        Query.Where(p => p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);
        Query.Select(p => new SiteMapInfo
        {
            Slug = p.RouteLink,
            CreateTimeUtc = p.PubDateUtc.GetValueOrDefault(),
            UpdateTimeUtc = p.LastModifiedUtc
        });
        Query.AsNoTracking();
    }
}
