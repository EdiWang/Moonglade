namespace Moonglade.Core.PageFeature;

public record ListPageSegmentQuery : IRequest<IReadOnlyList<PageSegment>>;

public class ListPageSegmentQueryHandler(IRepository<PageEntity> repo) : IRequestHandler<ListPageSegmentQuery, IReadOnlyList<PageSegment>>
{
    public Task<IReadOnlyList<PageSegment>> Handle(ListPageSegmentQuery request, CancellationToken ct)
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