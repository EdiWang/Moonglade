using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record GetAllThemeSegmentQuery : IRequest<List<BlogThemeEntity>>;

public class GetAllThemeSegmentQueryHandler(MoongladeRepository<BlogThemeEntity> repo) : IRequestHandler<GetAllThemeSegmentQuery, List<BlogThemeEntity>>
{
    public async Task<List<BlogThemeEntity>> Handle(GetAllThemeSegmentQuery request, CancellationToken ct)
    {
        var systemThemes = ThemeFactory.GetSystemThemes();
        var customThemes = await repo.ListAsync(ct);

        var result = new List<BlogThemeEntity>();

        result.AddRange(systemThemes);
        result.AddRange(customThemes);

        return result;
    }
}