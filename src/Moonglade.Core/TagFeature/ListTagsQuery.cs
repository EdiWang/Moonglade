using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Core.TagFeature;

public record ListTagsQuery : IQuery<List<TagEntity>>;

public class ListTagsQueryHandler(MoongladeRepository<TagEntity> repo) : IQueryHandler<ListTagsQuery, List<TagEntity>>
{
    public Task<List<TagEntity>> HandleAsync(ListTagsQuery request, CancellationToken ct) => repo.ListAsync(ct);
}