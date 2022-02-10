using Moonglade.Data.Spec;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryByRouteQuery(string RouteName) : IRequest<Category>;

public class GetCategoryByRouteQueryHandler : IRequestHandler<GetCategoryByRouteQuery, Category>
{
    private readonly IRepository<CategoryEntity> _catRepo;

    public GetCategoryByRouteQueryHandler(IRepository<CategoryEntity> catRepo)
    {
        _catRepo = catRepo;
    }

    public Task<Category> Handle(GetCategoryByRouteQuery request, CancellationToken cancellationToken)
    {
        return _catRepo.SelectFirstOrDefaultAsync(new CategorySpec(request.RouteName), Category.EntitySelector);
    }
}