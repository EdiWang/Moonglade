using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record ListWidgetsQuery : IQuery<List<WidgetEntity>>;

public class ListWidgetsQueryHandler(BlogDbContext db) : IQueryHandler<ListWidgetsQuery, List<WidgetEntity>>
{
    public Task<List<WidgetEntity>> HandleAsync(ListWidgetsQuery request, CancellationToken ct) =>
        db.Widget.AsNoTracking().ToListAsync(ct);
}