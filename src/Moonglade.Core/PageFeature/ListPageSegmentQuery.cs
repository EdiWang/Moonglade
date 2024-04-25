using Moonglade.Data;

namespace Moonglade.Core.PageFeature;

public record ListPageSegmentQuery : IRequest<List<PageSegment>>;

public class ListPageSegmentQueryHandler(MoongladeRepository<PageEntity> repo) : IRequestHandler<ListPageSegmentQuery, List<PageSegment>>
{
    public Task<List<PageSegment>> Handle(ListPageSegmentQuery request, CancellationToken ct)
    {
        return repo.SelectAsync(page => new PageSegment
        {
            Id = page.Id,
            CreateTimeUtc = page.CreateTimeUtc,
            Slug = page.Slug,
            Title = page.Title,
            IsPublished = page.IsPublished
        }, ct);
    }
}