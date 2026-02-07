using LiteBus.Queries.Abstractions;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Category;

public record GetCategoryBySlugQuery(string Slug) : IQuery<CategoryEntity>;

public class GetCategoryByRouteQueryHandler(IRepositoryBase<CategoryEntity> repo) : IQueryHandler<GetCategoryBySlugQuery, CategoryEntity>
{
    public Task<CategoryEntity> HandleAsync(GetCategoryBySlugQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new CategoryBySlugSpec(request.Slug), ct);
}