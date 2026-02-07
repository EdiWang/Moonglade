using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Category;

public record GetCategoryQuery(Guid Id) : IQuery<CategoryEntity>;

public class GetCategoryByIdQueryHandler(IRepositoryBase<CategoryEntity> repo) : IQueryHandler<GetCategoryQuery, CategoryEntity>
{
    public async Task<CategoryEntity> HandleAsync(GetCategoryQuery request, CancellationToken ct) =>
        await repo.GetByIdAsync(request.Id, ct);
}