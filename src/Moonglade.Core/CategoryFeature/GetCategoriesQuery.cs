using Edi.CacheAside.InMemory;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoriesQuery : IRequest<List<CategoryEntity>>;

public class GetCategoriesQueryHandler(MoongladeRepository<CategoryEntity> repo, ICacheAside cache) : IRequestHandler<GetCategoriesQuery, List<CategoryEntity>>
{
    public Task<List<CategoryEntity>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await repo.ListAsync(ct);
            return list;
        });
    }
}