using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public record ListArchiveQuery(int Year, int? Month = null) : IQuery<List<PostDigest>>;

public class ListArchiveQueryHandler(IRepositoryBase<PostEntity> repo) : IQueryHandler<ListArchiveQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListArchiveQuery request, CancellationToken ct)
    {
        var spec = new PostByYearMonthSpec(request.Year, request.Month.GetValueOrDefault());
        var dtoSpec = new PostEntityToDigestSpec();
        var newSpec = spec.WithProjectionOf(dtoSpec);

        var list = repo.ListAsync(newSpec, ct);
        return list;
    }
}