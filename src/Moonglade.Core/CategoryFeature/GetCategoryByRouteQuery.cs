
namespace Moonglade.Core.CategoryFeature;

public record GetCategoryByRouteQuery(string Slug) : IRequest<CategoryEntity>;

public class GetCategoryByRouteQueryHandler(IRepository<CategoryEntity> repo) : IRequestHandler<GetCategoryByRouteQuery, CategoryEntity>
{
    public Task<CategoryEntity> Handle(GetCategoryByRouteQuery request, CancellationToken ct) =>
        repo.GetAsync(p => p.Slug == request.Slug);
}