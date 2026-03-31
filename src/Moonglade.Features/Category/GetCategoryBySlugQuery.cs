using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Category;

public record GetCategoryBySlugQuery(string Slug) : IQuery<CategoryEntity>;

public class GetCategoryByRouteQueryHandler(BlogDbContext db) : IQueryHandler<GetCategoryBySlugQuery, CategoryEntity>
{
    public Task<CategoryEntity> HandleAsync(GetCategoryBySlugQuery request, CancellationToken ct) =>
        db.Category.FirstOrDefaultAsync(c => c.Slug == request.Slug, ct);
}