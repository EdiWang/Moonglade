namespace Moonglade.Core.PageFeature;

public record GetPageBySlugQuery(string Slug) : IRequest<BlogPage>;

public class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, BlogPage>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public GetPageBySlugQueryHandler(IRepository<PageEntity> pageRepo)
    {
        _pageRepo = pageRepo;
    }

    public async Task<BlogPage> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var lower = request.Slug.ToLower();
        var entity = await _pageRepo.GetAsync(p => p.Slug == lower);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}