using Edi.CacheAside.InMemory;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoriesQuery : IRequest<IReadOnlyList<Category>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
{
    private readonly IRepository<CategoryEntity> _repo;
    private readonly ICacheAside _cache;

    public GetCategoriesQueryHandler(IRepository<CategoryEntity> repo, ICacheAside cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return _cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await _repo.SelectAsync(Category.EntitySelector, ct);
            return list;
        });
    }
}