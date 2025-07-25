using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IQuery<List<PostDigest>>;

public class ListFeaturedQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<ListFeaturedQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var posts = repo.SelectAsync(new FeaturedPostPagingSpec(pageSize, pageIndex), PostDigest.EntitySelector, ct);
        return posts;
    }
}