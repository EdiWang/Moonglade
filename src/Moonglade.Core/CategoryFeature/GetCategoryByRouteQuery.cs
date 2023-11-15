using Moonglade.Data.Spec;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryByRouteQuery(string RouteName) : IRequest<Category>;

public class GetCategoryByRouteQueryHandler(IRepository<CategoryEntity> repo) : IRequestHandler<GetCategoryByRouteQuery, Category>
{
    public Task<Category> Handle(GetCategoryByRouteQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new CategorySpec(request.RouteName), Category.EntitySelector);
}