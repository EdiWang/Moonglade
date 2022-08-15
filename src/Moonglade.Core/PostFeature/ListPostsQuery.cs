using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class ListPostsQuery : IRequest<IReadOnlyList<PostDigest>>
{
    public ListPostsQuery(int pageSize, int pageIndex, Guid? catId = null, PostsSortBy sortBy = PostsSortBy.Recent)
    {
        PageSize = pageSize;
        PageIndex = pageIndex;
        CatId = catId;
        SortBy = sortBy;
    }

    public int PageSize { get; set; }

    public int PageIndex { get; set; }

    public Guid? CatId { get; set; }

    public PostsSortBy SortBy { get; set; } 
}

public class ListPostsQueryHandler : IRequestHandler<ListPostsQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _postRepo;

    public ListPostsQueryHandler(IRepository<PostEntity> postRepo) => _postRepo = postRepo;

    public Task<IReadOnlyList<PostDigest>> Handle(ListPostsQuery request, CancellationToken cancellationToken)
    {
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var spec = new PostPagingSpec(request.PageSize, request.PageIndex, request.CatId, request.SortBy);
        return _postRepo.SelectAsync(spec, PostDigest.EntitySelector);
    }
}