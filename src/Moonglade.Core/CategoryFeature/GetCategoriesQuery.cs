using Edi.CacheAside.InMemory;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoriesQuery : IRequest<IReadOnlyList<Category>>;

public class GetCategoriesQueryHandler(IRepository<CategoryEntity> repo, ICacheAside cache) : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
{
    public Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await repo.SelectAsync(Category.EntitySelector, ct);
            return list;
        });
    }
}