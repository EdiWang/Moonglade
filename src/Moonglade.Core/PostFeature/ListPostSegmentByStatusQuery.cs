using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IRequest<List<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler(IRepository<PostEntity> repo) : IRequestHandler<ListPostSegmentByStatusQuery, List<PostSegment>>
{
    public Task<List<PostSegment>> Handle(ListPostSegmentByStatusQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Status);
        return repo.SelectAsync(spec, PostSegment.EntitySelector, ct);
    }
}