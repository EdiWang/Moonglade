namespace Moonglade.Core.CategoryFeature;

public record GetCategoryQuery(Guid Id) : IRequest<CategoryEntity>;

public class GetCategoryByIdQueryHandler(IRepository<CategoryEntity> repo) : IRequestHandler<GetCategoryQuery, CategoryEntity>
{
    public async Task<CategoryEntity> Handle(GetCategoryQuery request, CancellationToken ct) =>
        await repo.GetAsync(request.Id, ct);
}