using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class ListPostsQuery(int pageSize, int pageIndex, Guid? catId = null)
    : IQuery<List<PostDigest>>
{
    public int PageSize { get; set; } = pageSize;

    public int PageIndex { get; set; } = pageIndex;

    public Guid? CatId { get; set; } = catId;
}

public class ListPostsQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<ListPostsQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListPostsQuery request, CancellationToken ct)
    {
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var spec = new PostPagingSpec(request.PageSize, request.PageIndex, request.CatId);
        return repo.SelectAsync(spec, PostDigest.EntitySelector, ct);
    }
}