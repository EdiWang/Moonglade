using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class BlogThemeForIdNameSpec : Specification<BlogThemeEntity, ThemeSegment>
{
    public BlogThemeForIdNameSpec()
    {
        Query.Select(p => new(p.Id, p.ThemeName));
    }
}