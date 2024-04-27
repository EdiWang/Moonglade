using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IRequest<List<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler(MoongladeRepository<PostEntity> repo) : IRequestHandler<ListPostSegmentByStatusQuery, List<PostSegment>>
{
    public Task<List<PostSegment>> Handle(ListPostSegmentByStatusQuery request, CancellationToken ct)
    {
        var spec = new PostByStatusSpec(request.Status);
        return repo.SelectAsync(spec, PostSegment.EntitySelector, ct);
    }
}