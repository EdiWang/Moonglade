using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Page;

public record ListPageSegmentsQuery(bool IncludeDeleted = false, bool DeletedOnly = false) : IQuery<List<PageSegment>>;

public class ListPageSegmentsQueryHandler(BlogDbContext db) : IQueryHandler<ListPageSegmentsQuery, List<PageSegment>>
{
    public Task<List<PageSegment>> HandleAsync(ListPageSegmentsQuery request, CancellationToken ct) =>
        db.BlogPage.AsNoTracking()
            .Where(page => request.IncludeDeleted || page.IsDeleted == request.DeletedOnly)
            .Select(page => new PageSegment
            {
                Id = page.Id,
                CreateTimeUtc = page.CreateTimeUtc,
                Slug = page.Slug,
                Title = page.Title,
                IsPublished = page.IsPublished,
                IsDeleted = page.IsDeleted
            })
            .ToListAsync(ct);
}