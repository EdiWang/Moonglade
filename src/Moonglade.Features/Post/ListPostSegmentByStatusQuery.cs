using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IQuery<List<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler(IRepositoryBase<PostEntity> repo) : IQueryHandler<ListPostSegmentByStatusQuery, List<PostSegment>>
{
    public Task<List<PostSegment>> HandleAsync(ListPostSegmentByStatusQuery request, CancellationToken ct)
    {
        var spec = new PostByStatusSpec(request.Status);
        var dtoSpec = new PostEntityToSegmentSpec();
        var newSpec = spec.WithProjectionOf(dtoSpec);

        return repo.ListAsync(newSpec, ct);
    }
}