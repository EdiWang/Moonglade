using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Theme;

public record GetAllThemeSegmentQuery : IRequest<List<ThemeSegment>>;

public class GetAllThemeSegmentQueryHandler(MoongladeRepository<BlogThemeEntity> repo) : IRequestHandler<GetAllThemeSegmentQuery, List<ThemeSegment>>
{
    public async Task<List<ThemeSegment>> Handle(GetAllThemeSegmentQuery request, CancellationToken ct)
    {
        var systemThemes = ThemeFactory.GetSystemThemes();
        var customThemes = await repo.ListAsync(new BlogThemeForIdNameSpec(), ct);

        var result = new List<ThemeSegment>();

        result.AddRange(systemThemes.Select(t => new ThemeSegment(t.Id, t.ThemeName)));
        result.AddRange(customThemes);

        return result;
    }
}