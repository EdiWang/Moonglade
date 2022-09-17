namespace Moonglade.Core.PageFeature;

public record GetPageByIdQuery(Guid Id) : IRequest<BlogPage>;

public class GetPageByIdQueryHandler : IRequestHandler<GetPageByIdQuery, BlogPage>
{
    private readonly IRepository<PageEntity> _repo;

    public GetPageByIdQueryHandler(IRepository<PageEntity> repo) => _repo = repo;

    public async Task<BlogPage> Handle(GetPageByIdQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(request.Id, ct);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}