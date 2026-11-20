using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record ListWidgetsQuery : IQuery<List<WidgetEntity>>;

public class ListWidgetsQueryHandler(BlogDbContext db) : IQueryHandler<ListWidgetsQuery, List<WidgetEntity>>
{
    public async Task<List<WidgetEntity>> HandleAsync(ListWidgetsQuery request, CancellationToken ct)
    {
        var widgets = await db.Widget.AsNoTracking().ToListAsync(ct);
        return widgets.OrderBy(widget => widget.Region)
            .ThenBy(widget => widget.SortOrder)
            .ThenBy(widget => widget.CreatedTimeUtc)
            .ThenBy(widget => widget.Id.ToString("D"), StringComparer.Ordinal)
            .ToList();
    }
}
