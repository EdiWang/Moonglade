using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.PageFeature;

public record GetPageBySlugQuery(string Slug) : IQuery<PageEntity>;

public class GetPageBySlugQueryHandler(MoongladeRepository<PageEntity> repo) : IQueryHandler<GetPageBySlugQuery, PageEntity>
{
    public async Task<PageEntity> HandleAsync(GetPageBySlugQuery request, CancellationToken ct)
    {
        var lower = request.Slug.ToLower();
        var entity = await repo.FirstOrDefaultAsync(new PageBySlugSpec(lower), ct);
        return entity;
    }
}