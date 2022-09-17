using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record ListInsightsQuery(PostInsightsType PostInsightsType) : IRequest<IReadOnlyList<PostSegment>>;

public class ListInsightsQueryHandler : IRequestHandler<ListInsightsQuery, IReadOnlyList<PostSegment>>
{
    private readonly IRepository<PostEntity> _repo;

    public ListInsightsQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<PostSegment>> Handle(ListInsightsQuery request, CancellationToken ct)
    {
        var spec = new PostInsightsSpec(request.PostInsightsType, 10);
        return _repo.SelectAsync(spec, PostSegment.EntitySelector);
    }
}