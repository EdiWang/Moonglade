using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Category;

public record GetCategoryQuery(Guid Id) : IQuery<CategoryEntity>;

public class GetCategoryByIdQueryHandler(BlogDbContext db) : IQueryHandler<GetCategoryQuery, CategoryEntity>
{
    public async Task<CategoryEntity> HandleAsync(GetCategoryQuery request, CancellationToken ct) =>
        await db.Category.FindAsync([request.Id], ct);
}