using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Page;

public record ListPageSegmentsQuery : IQuery<List<PageSegment>>;

public class ListPageSegmentsQueryHandler(MoongladeRepository<PageEntity> repo) : IQueryHandler<ListPageSegmentsQuery, List<PageSegment>>
{
    public Task<List<PageSegment>> HandleAsync(ListPageSegmentsQuery request, CancellationToken ct) =>
        repo.ListAsync(new PageSegmentSpec(), ct);
}