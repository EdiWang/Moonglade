using Moonglade.Data.Spec;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryByRouteQuery(string RouteName) : IRequest<Category>;

public class GetCategoryByRouteQueryHandler : IRequestHandler<GetCategoryByRouteQuery, Category>
{
    private readonly IRepository<CategoryEntity> _repo;
    public GetCategoryByRouteQueryHandler(IRepository<CategoryEntity> repo) => _repo = repo;

    public Task<Category> Handle(GetCategoryByRouteQuery request, CancellationToken ct) =>
        _repo.FirstOrDefaultAsync(new CategorySpec(request.RouteName), Category.EntitySelector);
}