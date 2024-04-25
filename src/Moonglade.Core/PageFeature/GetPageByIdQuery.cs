using Moonglade.Data;

namespace Moonglade.Core.PageFeature;

public record GetPageByIdQuery(Guid Id) : IRequest<BlogPage>;

public class GetPageByIdQueryHandler(MoongladeRepository<PageEntity> repo) : IRequestHandler<GetPageByIdQuery, BlogPage>
{
    public async Task<BlogPage> Handle(GetPageByIdQuery request, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity == null) return null;

        var item = new BlogPage(entity);
        return item;
    }
}