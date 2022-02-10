using Moonglade.Data.Spec;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryByIdQuery(Guid Id) : IRequest<Category>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Category>
{
    private readonly IRepository<CategoryEntity> _catRepo;

    public GetCategoryByIdQueryHandler(IRepository<CategoryEntity> catRepo)
    {
        _catRepo = catRepo;
    }

    public Task<Category> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        return _catRepo.SelectFirstOrDefaultAsync(new CategorySpec(request.Id), Category.EntitySelector);
    }
}