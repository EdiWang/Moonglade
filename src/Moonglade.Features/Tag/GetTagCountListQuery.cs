using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Tag;

public record GetTagCountListQuery : IQuery<List<TagWithCount>>;

public class GetTagCountListQueryHandler(BlogDbContext db) : IQueryHandler<GetTagCountListQuery, List<TagWithCount>>
{
    public Task<List<TagWithCount>> HandleAsync(GetTagCountListQuery request, CancellationToken ct) =>
        db.Tag.AsNoTracking()
            .Select(t => new TagWithCount
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName,
                PostCount = t.Posts.Count
            })
            .ToListAsync(ct);
}