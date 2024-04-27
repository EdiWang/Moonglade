using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PageFeature;

public record GetPageBySlugQuery(string Slug) : IRequest<BlogPage>;

public class GetPageBySlugQueryHandler(MoongladeRepository<PageEntity> repo) : IRequestHandler<GetPageBySlugQuery, BlogPage>
{
    public async Task<BlogPage> Handle(GetPageBySlugQuery request, CancellationToken ct)
    {
        var lower = request.Slug.ToLower();
        var entity = await repo.FirstOrDefaultAsync(new PageBySlugSpec(lower), ct);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}