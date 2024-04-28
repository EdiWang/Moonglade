using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryEntity>;

public class GetCategoryByRouteQueryHandler(MoongladeRepository<CategoryEntity> repo) : IRequestHandler<GetCategoryBySlugQuery, CategoryEntity>
{
    public Task<CategoryEntity> Handle(GetCategoryBySlugQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new CategoryBySlugSpec(request.Slug), ct);
}