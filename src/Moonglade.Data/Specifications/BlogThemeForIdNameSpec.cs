using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class BlogThemeForIdNameSpec : Specification<BlogThemeEntity, ThemeSegment>
{
    public BlogThemeForIdNameSpec()
    {
        Query.Select(p => new(p.Id, p.ThemeName));
        Query.AsNoTracking();
    }
}

public sealed class ThemeByTypeSpec : Specification<BlogThemeEntity>
{
    public ThemeByTypeSpec(ThemeType type)
    {
        Query.Where(p => p.ThemeType == type);
    }
}