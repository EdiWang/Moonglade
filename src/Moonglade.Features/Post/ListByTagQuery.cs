using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record ListByTagQuery(int TagId, int PageSize, int PageIndex) : IQuery<List<PostDigest>>;

public class ListByTagQueryHandler(IRepositoryBase<PostTagEntity> repo) : IQueryHandler<ListByTagQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListByTagQuery request, CancellationToken ct)
    {
        if (request.TagId <= 0) throw new ArgumentOutOfRangeException(nameof(request.TagId));
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var spec = new PostTagSpec(request.TagId, request.PageSize, request.PageIndex);
        var dtoSpec = new PostTagEntityToPostDigestSpec();
        var newSpec = spec.WithProjectionOf(dtoSpec);

        var posts = repo.ListAsync(newSpec, ct);
        return posts;
    }
}