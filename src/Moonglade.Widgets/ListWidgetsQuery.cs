using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record ListWidgetsQuery : IQuery<List<WidgetEntity>>;

public class ListWidgetsQueryHandler(BlogDbContext db, WidgetCollectionGate gate) : IQueryHandler<ListWidgetsQuery, List<WidgetEntity>>
{
    public async Task<List<WidgetEntity>> HandleAsync(ListWidgetsQuery request, CancellationToken ct)
    {
        var widgets = await gate.ReadAsync(token => db.Widget.AsNoTracking().ToListAsync(token), ct);
        return widgets.OrderBy(widget => widget.Region)
            .ThenBy(widget => widget.SortOrder)
            .ThenBy(widget => widget.CreatedTimeUtc)
            .ThenBy(widget => widget.Id.ToString("D"), StringComparer.Ordinal)
            .ToList();
    }
}
