using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Theme;

public record GetAllThemeSegmentQuery : IRequest<List<ThemeSegment>>;

public class GetAllThemeSegmentQueryHandler(MoongladeRepository<BlogThemeEntity> repo) : IRequestHandler<GetAllThemeSegmentQuery, List<ThemeSegment>>
{
    public Task<List<ThemeSegment>> Handle(GetAllThemeSegmentQuery request, CancellationToken ct) =>
        repo.ListAsync(new BlogThemeForIdNameSpec(), ct);
}