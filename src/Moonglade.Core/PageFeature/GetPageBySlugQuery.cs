namespace Moonglade.Core.PageFeature;

public record GetPageBySlugQuery(string Slug) : IRequest<BlogPage>;

public class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, BlogPage>
{
    private readonly IRepository<PageEntity> _repo;

    public GetPageBySlugQueryHandler(IRepository<PageEntity> repo) => _repo = repo;

    public async Task<BlogPage> Handle(GetPageBySlugQuery request, CancellationToken ct)
    {
        var lower = request.Slug.ToLower();
        var entity = await _repo.GetAsync(p => p.Slug == lower);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}