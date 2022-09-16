using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IRequest<IReadOnlyList<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler : IRequestHandler<ListPostSegmentByStatusQuery, IReadOnlyList<PostSegment>>
{
    private readonly IRepository<PostEntity> _repo;
    public ListPostSegmentByStatusQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<PostSegment>> Handle(ListPostSegmentByStatusQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Status);
        return _repo.SelectAsync(spec, PostSegment.EntitySelector);
    }
}