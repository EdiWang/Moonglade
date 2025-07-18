using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IQuery<List<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<ListPostSegmentByStatusQuery, List<PostSegment>>
{
    public Task<List<PostSegment>> HandleAsync(ListPostSegmentByStatusQuery request, CancellationToken ct)
    {
        var spec = new PostByStatusSpec(request.Status);
        return repo.SelectAsync(spec, PostSegment.EntitySelector, ct);
    }
}