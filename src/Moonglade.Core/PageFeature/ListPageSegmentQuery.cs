using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PageFeature;

public record ListPageSegmentQuery : IQuery<List<PageSegment>>;

public class ListPageSegmentQueryHandler(MoongladeRepository<PageEntity> repo) : IQueryHandler<ListPageSegmentQuery, List<PageSegment>>
{
    public Task<List<PageSegment>> HandleAsync(ListPageSegmentQuery request, CancellationToken ct) =>
        repo.ListAsync(new PageSegmentSpec(), ct);
}