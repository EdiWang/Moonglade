using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features.Category;

public record GetCategoryQuery(Guid Id) : IQuery<CategoryEntity>;

public class GetCategoryByIdQueryHandler(MoongladeRepository<CategoryEntity> repo) : IQueryHandler<GetCategoryQuery, CategoryEntity>
{
    public async Task<CategoryEntity> HandleAsync(GetCategoryQuery request, CancellationToken ct) =>
        await repo.GetByIdAsync(request.Id, ct);
}