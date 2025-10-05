using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record ListByTagQuery(int TagId, int PageSize, int PageIndex) : IQuery<List<PostDigest>>;

public class ListByTagQueryHandler(MoongladeRepository<PostTagEntity> repo) : IQueryHandler<ListByTagQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListByTagQuery request, CancellationToken ct)
    {
        if (request.TagId <= 0) throw new ArgumentOutOfRangeException(nameof(request.TagId));
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var posts = repo.SelectAsync(new PostTagSpec(request.TagId, request.PageSize, request.PageIndex), PostDigest.EntitySelectorByTag, ct);
        return posts;
    }
}