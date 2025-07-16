using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Core.TagFeature;

public record GetTagsQuery : IQuery<List<TagEntity>>;

public class GetTagsQueryHandler(MoongladeRepository<TagEntity> repo) : IQueryHandler<GetTagsQuery, List<TagEntity>>
{
    public Task<List<TagEntity>> HandleAsync(GetTagsQuery request, CancellationToken ct) => repo.ListAsync(ct);
}