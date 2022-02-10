using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListFeaturedQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListFeaturedQueryHandler : IRequestHandler<ListFeaturedQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _postRepo;

    public ListFeaturedQueryHandler(IRepository<PostEntity> postRepo)
    {
        _postRepo = postRepo;
    }

    public Task<IReadOnlyList<PostDigest>> Handle(ListFeaturedQuery request, CancellationToken cancellationToken)
    {
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var posts = _postRepo.SelectAsync(new FeaturedPostSpec(request.PageSize, request.PageIndex), PostDigest.EntitySelector);
        return posts;
    }
}