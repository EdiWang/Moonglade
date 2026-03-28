using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Tag;

public record ListTopTagsQuery(int Top) : IQuery<List<(TagEntity Tag, int PostCount)>>;

public class ListTopTagsQueryHandler(BlogDbContext db) : IQueryHandler<ListTopTagsQuery, List<(TagEntity Tag, int PostCount)>>
{
    public async Task<List<(TagEntity Tag, int PostCount)>> HandleAsync(ListTopTagsQuery request, CancellationToken ct)
    {
        if (!await db.Tag.AnyAsync(ct)) return [];

        var tags = await db.Tag.AsNoTracking()
            .OrderByDescending(t => t.Posts.Count)
            .Take(request.Top)
            .Select(t => new ValueTuple<TagEntity, int>(t, t.Posts.Count))
            .ToListAsync(ct);

        return tags;
    }
}