using Moonglade.Data.Spec;

namespace Moonglade.Core.PageFeature;

public record GetPagesQuery(int Top) : IRequest<IReadOnlyList<BlogPage>>;

public class GetPagesQueryHandler : IRequestHandler<GetPagesQuery, IReadOnlyList<BlogPage>>
{
    private readonly IRepository<PageEntity> _repo;

    public GetPagesQueryHandler(IRepository<PageEntity> repo) => _repo = repo;

    public async Task<IReadOnlyList<BlogPage>> Handle(GetPagesQuery request, CancellationToken ct)
    {
        var pages = await _repo.ListAsync(new PageSpec(request.Top));
        var list = pages.Select(p => new BlogPage(p)).ToList();
        return list;
    }
}