using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public class ListPostSegmentByStatusQuery : IRequest<IReadOnlyList<PostSegment>>
{
    public ListPostSegmentByStatusQuery(PostStatus status)
    {
        Status = status;
    }

    public PostStatus Status { get; set; }
}

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