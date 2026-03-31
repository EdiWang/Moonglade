using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public record ListThemeSegmentsQuery : IQuery<List<BlogThemeEntity>>;

public class ListThemeSegmentsQueryHandler(BlogDbContext db) : IQueryHandler<ListThemeSegmentsQuery, List<BlogThemeEntity>>
{
    public async Task<List<BlogThemeEntity>> HandleAsync(ListThemeSegmentsQuery request, CancellationToken ct)
    {
        var systemThemes = ThemeFactory.GetSystemThemes();
        var customThemes = await db.BlogTheme.AsNoTracking().ToListAsync(ct);

        var result = new List<BlogThemeEntity>();

        result.AddRange(systemThemes);
        result.AddRange(customThemes);

        return result;
    }
}