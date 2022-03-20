using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListFeaturedQueryHandler : IRequestHandler<ListFeaturedQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _postRepo;

    public ListFeaturedQueryHandler(IRepository<PostEntity> postRepo) => _postRepo = postRepo;

    public Task<IReadOnlyList<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken cancellationToken)
    {
        var (pageSize, pageIndex) = request;
        Helper.ValidatePagingParameters(pageSize, pageIndex);

        var posts = _postRepo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex), PostDigest.EntitySelector);
        return posts;
    }
}