namespace Moonglade.Core.PageFeature;

public record GetPageByIdQuery(Guid Id) : IRequest<BlogPage>;

public class GetPageByIdQueryHandler : IRequestHandler<GetPageByIdQuery, BlogPage>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public GetPageByIdQueryHandler(IRepository<PageEntity> pageRepo)
    {
        _pageRepo = pageRepo;
    }

    public async Task<BlogPage> Handle(GetPageByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _pageRepo.GetAsync(request.Id);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}