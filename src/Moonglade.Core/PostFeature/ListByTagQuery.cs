using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListByTagQuery(int TagId, int PageSize, int PageIndex) : IRequest<IReadOnlyList<PostDigest>>;

public class ListByTagQueryHandler(IRepository<PostTagEntity> repo) : IRequestHandler<ListByTagQuery, IReadOnlyList<PostDigest>>
{
    public Task<IReadOnlyList<PostDigest>> Handle(ListByTagQuery request, CancellationToken ct)
    {
        if (request.TagId <= 0) throw new ArgumentOutOfRangeException(nameof(request.TagId));
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var posts = repo.SelectAsync(new PostTagSpec(request.TagId, request.PageSize, request.PageIndex), PostDigest.EntitySelectorByTag, ct);
        return posts;
    }
}