using Ardalis.Specification;
using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Features.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IQuery<List<PostDigest>>;

public class ListFeaturedQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<ListFeaturedQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var spec = new FeaturedPostPagingSpec(pageSize, pageIndex);
        var dtoSpec = new PostEntityToDigestSpec();
        var newSpec = spec.WithProjectionOf(dtoSpec);

        var posts = repo.ListAsync(newSpec, ct);
        return posts;
    }
}