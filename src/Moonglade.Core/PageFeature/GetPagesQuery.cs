using Moonglade.Data.Spec;

namespace Moonglade.Core.PageFeature;

public record GetPagesQuery(int Top) : IRequest<IReadOnlyList<BlogPage>>;

public class GetPagesQueryHandler : IRequestHandler<GetPagesQuery, IReadOnlyList<BlogPage>>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public GetPagesQueryHandler(IRepository<PageEntity> pageRepo) => _pageRepo = pageRepo;

    public async Task<IReadOnlyList<BlogPage>> Handle(GetPagesQuery request, CancellationToken cancellationToken)
    {
        var pages = await _pageRepo.ListAsync(new PageSpec(request.Top));
        var list = pages.Select(p => new BlogPage(p)).ToList();
        return list;
    }
}