﻿using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PageFeature;

public record ListPageSegmentsQuery : IQuery<List<PageSegment>>;

public class ListPageSegmentsQueryHandler(MoongladeRepository<PageEntity> repo) : IQueryHandler<ListPageSegmentsQuery, List<PageSegment>>
{
    public Task<List<PageSegment>> HandleAsync(ListPageSegmentsQuery request, CancellationToken ct) =>
        repo.ListAsync(new PageSegmentSpec(), ct);
}