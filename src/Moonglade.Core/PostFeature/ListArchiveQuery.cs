using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record ListArchiveQuery(int Year, int? Month = null) : IQuery<List<PostDigest>>;

public class ListArchiveQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<ListArchiveQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListArchiveQuery request, CancellationToken ct)
    {
        var spec = new PostByYearMonthSpec(request.Year, request.Month.GetValueOrDefault());
        var list = repo.SelectAsync(spec, PostDigest.EntitySelector, ct);
        return list;
    }
}