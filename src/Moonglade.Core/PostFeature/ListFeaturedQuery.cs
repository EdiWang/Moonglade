using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListFeaturedQueryHandler : IRequestHandler<ListFeaturedQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _repo;

    public ListFeaturedQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken ct)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var posts = _repo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex), PostDigest.EntitySelector, ct);
        return posts;
    }
}