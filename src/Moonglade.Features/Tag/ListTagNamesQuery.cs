using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Tag;

public record ListTagNamesQuery : IQuery<List<string>>;

public class ListTagNamesQueryHandler(BlogDbContext db) : IQueryHandler<ListTagNamesQuery, List<string>>
{
    public Task<List<string>> HandleAsync(ListTagNamesQuery request, CancellationToken ct) =>
        db.Tag.AsNoTracking().Select(t => t.DisplayName).ToListAsync(ct);
}