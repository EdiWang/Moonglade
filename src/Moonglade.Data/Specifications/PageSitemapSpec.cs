using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PageSitemapSpec : Specification<PageEntity, SiteMapInfo>
{
    public PageSitemapSpec()
    {
        Query.Where(p => p.IsPublished);
        Query.Select(p => new SiteMapInfo
        {
            Slug = p.Slug,
            CreateTimeUtc = p.CreateTimeUtc,
            UpdateTimeUtc = p.UpdateTimeUtc
        });
        Query.AsNoTracking();
    }
}