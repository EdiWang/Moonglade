using Moonglade.Data.Spec;

namespace Moonglade.Core.PageFeature;

public record GetPagesQuery(int Top) : IRequest<IReadOnlyList<BlogPage>>;

public class GetPagesQueryHandler(IRepository<PageEntity> repo) : IRequestHandler<GetPagesQuery, IReadOnlyList<BlogPage>>
{
    public async Task<IReadOnlyList<BlogPage>> Handle(GetPagesQuery request, CancellationToken ct)
    {
        var pages = await repo.ListAsync(new PageSpec(request.Top));
        var list = pages.Select(p => new BlogPage(p)).ToList();
        return list;
    }
}