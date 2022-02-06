using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record ListPostSegmentByStatusQuery(PostStatus Status) : IRequest<IReadOnlyList<PostSegment>>;

public class ListPostSegmentByStatusQueryHandler : IRequestHandler<ListPostSegmentByStatusQuery, IReadOnlyList<PostSegment>>
{
    private readonly IRepository<PostEntity> _postRepo;

    public ListPostSegmentByStatusQueryHandler(IRepository<PostEntity> postRepo)
    {
        _postRepo = postRepo;
    }

    public Task<IReadOnlyList<PostSegment>> Handle(ListPostSegmentByStatusQuery request, CancellationToken cancellationToken)
    {
        var spec = new PostSpec(request.Status);
        return _postRepo.SelectAsync(spec, PostSegment.EntitySelector);
    }
}