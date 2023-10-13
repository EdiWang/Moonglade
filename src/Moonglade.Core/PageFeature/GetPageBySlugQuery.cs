namespace Moonglade.Core.PageFeature;

public record GetPageBySlugQuery(string Slug) : IRequest<BlogPage>;

public class GetPageBySlugQueryHandler(IRepository<PageEntity> repo) : IRequestHandler<GetPageBySlugQuery, BlogPage>
{
    public async Task<BlogPage> Handle(GetPageBySlugQuery request, CancellationToken ct)
    {
        var lower = request.Slug.ToLower();
        var entity = await repo.GetAsync(p => p.Slug == lower);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}