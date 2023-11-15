using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IRequest<IReadOnlyList<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler(IRepository<PostEntity> repo) : IRequestHandler<ListPostSegmentByStatusQuery, IReadOnlyList<PostSegment>>
{
    public Task<IReadOnlyList<PostSegment>> Handle(ListPostSegmentByStatusQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Status);
        return repo.SelectAsync(spec, PostSegment.EntitySelector, ct);
    }
}