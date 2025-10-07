using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features.Category;

public record ListCategoriesQuery : IQuery<List<CategoryEntity>>;

public class ListCategoriesQueryHandler(MoongladeRepository<CategoryEntity> repo) : IQueryHandler<ListCategoriesQuery, List<CategoryEntity>>
{
    public Task<List<CategoryEntity>> HandleAsync(ListCategoriesQuery request, CancellationToken ct) => repo.ListAsync(ct);
}