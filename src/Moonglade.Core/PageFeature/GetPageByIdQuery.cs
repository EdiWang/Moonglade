namespace Moonglade.Core.PageFeature;

public record GetPageByIdQuery(Guid Id) : IRequest<BlogPage>;

public class GetPageByIdQueryHandler(IRepository<PageEntity> repo) : IRequestHandler<GetPageByIdQuery, BlogPage>
{
    public async Task<BlogPage> Handle(GetPageByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetAsync(request.Id, ct);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}