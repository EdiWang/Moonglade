using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryBySlugQuery(string Slug) : IQuery<CategoryEntity>;

public class GetCategoryByRouteQueryHandler(MoongladeRepository<CategoryEntity> repo) : IQueryHandler<GetCategoryBySlugQuery, CategoryEntity>
{
    public Task<CategoryEntity> HandleAsync(GetCategoryBySlugQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new CategoryBySlugSpec(request.Slug), ct);
}