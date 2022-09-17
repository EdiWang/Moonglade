using Moonglade.Caching;

namespace Moonglade.Core.CategoryFeature;

public record GetCategoriesQuery : IRequest<IReadOnlyList<Category>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
{
    private readonly IRepository<CategoryEntity> _repo;
    private readonly IBlogCache _cache;

    public GetCategoriesQueryHandler(IRepository<CategoryEntity> repo, IBlogCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return _cache.GetOrCreateAsync(CacheDivision.General, "allcats", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            var list = await _repo.SelectAsync(Category.EntitySelector);
            return list;
        });
    }
}