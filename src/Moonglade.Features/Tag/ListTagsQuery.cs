using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Tag;

public record ListTagsQuery : IQuery<List<TagEntity>>;

public class ListTagsQueryHandler(BlogDbContext db) : IQueryHandler<ListTagsQuery, List<TagEntity>>
{
    public Task<List<TagEntity>> HandleAsync(ListTagsQuery request, CancellationToken ct) =>
        db.Tag.AsNoTracking().ToListAsync(ct);
}