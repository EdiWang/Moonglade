using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class ListByTagQuery : IRequest<IReadOnlyList<PostDigest>>
{
    public ListByTagQuery(int tagId, int pageSize, int pageIndex)
    {
        TagId = tagId;
        PageSize = pageSize;
        PageIndex = pageIndex;
    }

    public int TagId { get; set; }

    public int PageSize { get; set; }

    public int PageIndex { get; set; }
}

public class ListByTagQueryHandler : IRequestHandler<ListByTagQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostTagEntity> _postTagRepo;

    public ListByTagQueryHandler(IRepository<PostTagEntity> postTagRepo)
    {
        _postTagRepo = postTagRepo;
    }

    public Task<IReadOnlyList<PostDigest>> Handle(ListByTagQuery request, CancellationToken cancellationToken)
    {
        if (request.TagId <= 0) throw new ArgumentOutOfRangeException(nameof(request.TagId));
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var posts = _postTagRepo.SelectAsync(new PostTagSpec(request.TagId, request.PageSize, request.PageIndex), PostDigest.EntitySelectorByTag);
        return posts;
    }
}