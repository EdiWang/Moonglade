using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PageFeature;

public record GetPageBySlugQuery(string Slug) : IRequest<PageEntity>;

public class GetPageBySlugQueryHandler(MoongladeRepository<PageEntity> repo) : IRequestHandler<GetPageBySlugQuery, PageEntity>
{
    public async Task<PageEntity> Handle(GetPageBySlugQuery request, CancellationToken ct)
    {
        var lower = request.Slug.ToLower();
        var entity = await repo.FirstOrDefaultAsync(new PageBySlugSpec(lower), ct);
        return entity;
    }
}