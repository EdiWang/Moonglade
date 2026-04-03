using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Tag;

public record ListTopTagsQuery(int Top) : IQuery<List<TagWithCount>>;

public class ListTopTagsQueryHandler(BlogDbContext db) : IQueryHandler<ListTopTagsQuery, List<TagWithCount>>
{
    public async Task<List<TagWithCount>> HandleAsync(ListTopTagsQuery request, CancellationToken ct)
    {
        if (!await db.Tag.AnyAsync(ct)) return [];

        var tags = await db.Tag.AsNoTracking()
            .OrderByDescending(t => t.Posts.Count)
            .Take(request.Top)
            .Select(t => new TagWithCount
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName,
                PostCount = t.Posts.Count
            })
            .ToListAsync(ct);

        return tags;
    }
}