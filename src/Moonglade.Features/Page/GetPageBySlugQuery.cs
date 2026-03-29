using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Page;

public record GetPageBySlugQuery(string Slug) : IQuery<PageEntity>;

public class GetPageBySlugQueryHandler(BlogDbContext db) : IQueryHandler<GetPageBySlugQuery, PageEntity>
{
    public async Task<PageEntity> HandleAsync(GetPageBySlugQuery request, CancellationToken ct)
    {
        var lower = request.Slug.ToLower();
        var entity = await db.BlogPage.FirstOrDefaultAsync(p => p.Slug == lower, ct);
        return entity;
    }
}