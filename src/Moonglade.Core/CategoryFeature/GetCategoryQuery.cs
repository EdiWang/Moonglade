using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoryQuery(Guid Id) : IRequest<CategoryEntity>;

public class GetCategoryByIdQueryHandler(MoongladeRepository<CategoryEntity> repo) : IRequestHandler<GetCategoryQuery, CategoryEntity>
{
    public async Task<CategoryEntity> Handle(GetCategoryQuery request, CancellationToken ct) =>
        await repo.GetByIdAsync(request.Id, ct);
}