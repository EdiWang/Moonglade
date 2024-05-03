using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PageSegmentSpec : Specification<PageEntity, PageSegment>
{
    public PageSegmentSpec()
    {
        Query.Select(page => new PageSegment
        {
            Id = page.Id,
            CreateTimeUtc = page.CreateTimeUtc,
            Slug = page.Slug,
            Title = page.Title,
            IsPublished = page.IsPublished
        });
        Query.AsNoTracking();
    }
}