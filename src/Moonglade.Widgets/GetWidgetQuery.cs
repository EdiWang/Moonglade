using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Moonglade.Widgets;

public record GetWidgetQuery(Guid Id) : IQuery<WidgetEntity>;

public class GetWidgetQueryHandler(BlogDbContext db) : IQueryHandler<GetWidgetQuery, WidgetEntity>
{
    public Task<WidgetEntity> HandleAsync(GetWidgetQuery request, CancellationToken ct) =>
        db.Widget.AsNoTracking().FirstOrDefaultAsync(w => w.Id == request.Id, ct);
}