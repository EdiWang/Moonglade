using Moonglade.Data.Spec;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryByIdQuery(Guid Id) : IRequest<Category>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Category>
{
    private readonly IRepository<CategoryEntity> _repo;

    public GetCategoryByIdQueryHandler(IRepository<CategoryEntity> repo) => _repo = repo;

    public Task<Category> Handle(GetCategoryByIdQuery request, CancellationToken ct) =>
        _repo.SelectFirstOrDefaultAsync(new CategorySpec(request.Id), Category.EntitySelector);
}