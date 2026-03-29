using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Page;

public record ListPageSegmentsQuery : IQuery<List<PageSegment>>;

public class ListPageSegmentsQueryHandler(BlogDbContext db) : IQueryHandler<ListPageSegmentsQuery, List<PageSegment>>
{
    public Task<List<PageSegment>> HandleAsync(ListPageSegmentsQuery request, CancellationToken ct) =>
        db.BlogPage.AsNoTracking()
            .Select(page => new PageSegment
            {
                Id = page.Id,
                CreateTimeUtc = page.CreateTimeUtc,
                Slug = page.Slug,
                Title = page.Title,
                IsPublished = page.IsPublished
            })
            .ToListAsync(ct);
}