
namespace Moonglade.Core.CategoryFeature;

public record GetCategoryByRouteQuery(string RouteName) : IRequest<CategoryEntity>;

public class GetCategoryByRouteQueryHandler(IRepository<CategoryEntity> repo) : IRequestHandler<GetCategoryByRouteQuery, CategoryEntity>
{
    public Task<CategoryEntity> Handle(GetCategoryByRouteQuery request, CancellationToken ct) =>
        repo.GetAsync(p => p.RouteName == request.RouteName);
}