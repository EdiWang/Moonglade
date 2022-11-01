namespace Moonglade.Core.PageFeature;

public record ListPageSegmentQuery : IRequest<IReadOnlyList<PageSegment>>;

public class ListPageSegmentQueryHandler : IRequestHandler<ListPageSegmentQuery, IReadOnlyList<PageSegment>>
{
    private readonly IRepository<PageEntity> _repo;

    public ListPageSegmentQueryHandler(IRepository<PageEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<PageSegment>> Handle(ListPageSegmentQuery request, CancellationToken ct)
    {
        return _repo.SelectAsync(page => new PageSegment
        {
            Id = page.Id,
            CreateTimeUtc = page.CreateTimeUtc,
            Slug = page.Slug,
            Title = page.Title,
            IsPublished = page.IsPublished
        }, ct);
    }
}