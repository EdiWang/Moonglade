using Edi.CacheAside.InMemory;
using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features.CategoryFeature;

public record ListCategoriesQuery : IQuery<List<CategoryEntity>>;

public class ListCategoriesQueryHandler(MoongladeRepository<CategoryEntity> repo, ICacheAside cache) : IQueryHandler<ListCategoriesQuery, List<CategoryEntity>>
{
    public Task<List<CategoryEntity>> HandleAsync(ListCategoriesQuery request, CancellationToken ct)
    {
        return cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await repo.ListAsync(ct);
            return list;
        });
    }
}