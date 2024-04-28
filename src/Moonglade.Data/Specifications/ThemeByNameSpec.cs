using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class ThemeByNameSpec : Specification<BlogThemeEntity>
{
    public ThemeByNameSpec(string name)
    {
        Query.Where(p => p.ThemeName == name);
    }
}