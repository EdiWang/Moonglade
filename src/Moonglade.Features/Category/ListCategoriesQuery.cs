using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Category;

public record ListCategoriesQuery : IQuery<List<CategoryEntity>>;

public class ListCategoriesQueryHandler(BlogDbContext db) : IQueryHandler<ListCategoriesQuery, List<CategoryEntity>>
{
    public Task<List<CategoryEntity>> HandleAsync(ListCategoriesQuery request, CancellationToken ct) =>
        db.Category.ToListAsync(ct);
}