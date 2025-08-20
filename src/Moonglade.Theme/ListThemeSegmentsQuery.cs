using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record ListThemeSegmentsQuery : IQuery<List<BlogThemeEntity>>;

public class ListThemeSegmentsQueryHandler(MoongladeRepository<BlogThemeEntity> repo) : IQueryHandler<ListThemeSegmentsQuery, List<BlogThemeEntity>>
{
    public async Task<List<BlogThemeEntity>> HandleAsync(ListThemeSegmentsQuery request, CancellationToken ct)
    {
        var systemThemes = ThemeFactory.GetSystemThemes();
        var customThemes = await repo.ListAsync(ct);

        var result = new List<BlogThemeEntity>();

        result.AddRange(systemThemes);
        result.AddRange(customThemes);

        return result;
    }
}