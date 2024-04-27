using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IRequest<List<PostDigest>>;

public class ListFeaturedQueryHandler(MoongladeRepository<PostEntity> repo) : IRequestHandler<ListFeaturedQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var posts = repo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex), PostDigest.EntitySelector, ct);
        return posts;
    }
}