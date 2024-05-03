using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PageFeature;

public record ListPageSegmentQuery : IRequest<List<PageSegment>>;

public class ListPageSegmentQueryHandler(MoongladeRepository<PageEntity> repo) : IRequestHandler<ListPageSegmentQuery, List<PageSegment>>
{
    public Task<List<PageSegment>> Handle(ListPageSegmentQuery request, CancellationToken ct) => 
        repo.ListAsync(new PageSegmentSpec(), ct);
}