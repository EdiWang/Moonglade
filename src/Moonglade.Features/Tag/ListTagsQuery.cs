using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Tag;

public record ListTagsQuery : IQuery<List<TagEntity>>;

public class ListTagsQueryHandler(IRepositoryBase<TagEntity> repo) : IQueryHandler<ListTagsQuery, List<TagEntity>>
{
    public Task<List<TagEntity>> HandleAsync(ListTagsQuery request, CancellationToken ct) => repo.ListAsync(ct);
}