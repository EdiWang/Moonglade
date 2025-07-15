using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record GetAllThemeSegmentQuery : IQuery<List<BlogThemeEntity>>;

public class GetAllThemeSegmentQueryHandler(MoongladeRepository<BlogThemeEntity> repo) : IQueryHandler<GetAllThemeSegmentQuery, List<BlogThemeEntity>>
{
    public async Task<List<BlogThemeEntity>> HandleAsync(GetAllThemeSegmentQuery request, CancellationToken ct)
    {
        var systemThemes = ThemeFactory.GetSystemThemes();
        var customThemes = await repo.ListAsync(ct);

        var result = new List<BlogThemeEntity>();

        result.AddRange(systemThemes);
        result.AddRange(customThemes);

        return result;
    }
}