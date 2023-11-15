using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListFeaturedQueryHandler(IRepository<PostEntity> repo) : IRequestHandler<ListFeaturedQuery, IReadOnlyList<PostDigest>>
{
    public Task<IReadOnlyList<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var posts = repo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex), PostDigest.EntitySelector, ct);
        return posts;
    }
}