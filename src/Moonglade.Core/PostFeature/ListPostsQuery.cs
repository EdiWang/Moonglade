using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class ListPostsQuery : IRequest<IReadOnlyList<PostDigest>>
{
    public ListPostsQuery(int pageSize, int pageIndex, Guid? catId = null)
    {
        PageSize = pageSize;
        PageIndex = pageIndex;
        CatId = catId;
    }

    public int PageSize { get; set; }

    public int PageIndex { get; set; }

    public Guid? CatId { get; set; }
}

public class ListPostsQueryHandler : IRequestHandler<ListPostsQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _postRepo;

    public ListPostsQueryHandler(IRepository<PostEntity> postRepo)
    {
        _postRepo = postRepo;
    }

    public Task<IReadOnlyList<PostDigest>> Handle(ListPostsQuery request, CancellationToken cancellationToken)
    {
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var spec = new PostPagingSpec(request.PageSize, request.PageIndex, request.CatId);
        return _postRepo.SelectAsync(spec, PostDigest.EntitySelector);
    }
}