using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core.PageFeature;

public class GetPagesQuery : IRequest<IReadOnlyList<BlogPage>>
{
    public GetPagesQuery(int top)
    {
        Top = top;
    }

    public int Top { get; set; }
}

public class GetPagesQueryHandler : IRequestHandler<GetPagesQuery, IReadOnlyList<BlogPage>>
{
    private readonly IRepository<PageEntity> _pageRepo;

    public GetPagesQueryHandler(IRepository<PageEntity> pageRepo)
    {
        _pageRepo = pageRepo;
    }

    public async Task<IReadOnlyList<BlogPage>> Handle(GetPagesQuery request, CancellationToken cancellationToken)
    {
        if (request.Top <= 0) throw new ArgumentOutOfRangeException(nameof(request.Top));

        var pages = await _pageRepo.GetAsync(new PageSpec(request.Top));
        var list = pages.Select(p => new BlogPage(p)).ToList();
        return list;
    }
}