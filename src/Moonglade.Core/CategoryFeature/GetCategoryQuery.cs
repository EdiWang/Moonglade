using Moonglade.Data.Spec;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryQuery(Guid Id) : IRequest<Category>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryQuery, Category>
{
    private readonly IRepository<CategoryEntity> _repo;

    public GetCategoryByIdQueryHandler(IRepository<CategoryEntity> repo) => _repo = repo;

    public Task<Category> Handle(GetCategoryQuery request, CancellationToken ct) =>
        _repo.FirstOrDefaultAsync(new CategorySpec(request.Id), Category.EntitySelector);
}