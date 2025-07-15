using Edi.CacheAside.InMemory;
using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoriesQuery : IQuery<List<CategoryEntity>>;

public class GetCategoriesQueryHandler(MoongladeRepository<CategoryEntity> repo, ICacheAside cache) : IQueryHandler<GetCategoriesQuery, List<CategoryEntity>>
{
    public Task<List<CategoryEntity>> HandleAsync(GetCategoriesQuery request, CancellationToken ct)
    {
        return cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await repo.ListAsync(ct);
            return list;
        });
    }
}