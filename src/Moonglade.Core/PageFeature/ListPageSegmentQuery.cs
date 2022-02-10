namespace Moonglade.Core.PageFeature;

public record ListPageSegmentQuery : IRequest<IReadOnlyList<PageSegment>>;

public class ListPageSegmentQueryHandler : IRequestHandler<ListPageSegmentQuery, IReadOnlyList<PageSegment>>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public ListPageSegmentQueryHandler(IRepository<PageEntity> pageRepo)
    {
        _pageRepo = pageRepo;
    }

    public Task<IReadOnlyList<PageSegment>> Handle(ListPageSegmentQuery request, CancellationToken cancellationToken)
    {
        return _pageRepo.SelectAsync(page => new PageSegment
        {
            Id = page.Id,
            CreateTimeUtc = page.CreateTimeUtc,
            Slug = page.Slug,
            Title = page.Title,
            IsPublished = page.IsPublished
        });
    }
}