using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class ListPostsQuery(int pageSize, int pageIndex, Guid? catId = null)
    : IRequest<IReadOnlyList<PostDigest>>
{
    public int PageSize { get; set; } = pageSize;

    public int PageIndex { get; set; } = pageIndex;

    public Guid? CatId { get; set; } = catId;
}

public class ListPostsQueryHandler(IRepository<PostEntity> repo) : IRequestHandler<ListPostsQuery, IReadOnlyList<PostDigest>>
{
    public Task<IReadOnlyList<PostDigest>> Handle(ListPostsQuery request, CancellationToken ct)
    {
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var spec = new PostPagingSpec(request.PageSize, request.PageIndex, request.CatId);
        return repo.SelectAsync(spec, PostDigest.EntitySelector, ct);
    }
}