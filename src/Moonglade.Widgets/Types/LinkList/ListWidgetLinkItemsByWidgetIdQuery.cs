using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets.Types.LinkList;

public record ListWidgetLinkItemsByWidgetIdQuery(Guid WidgetId) : IQuery<List<WidgetLinkItemEntity>>;

public class ListWidgetLinkItemsByWidgetIdQueryHandler(MoongladeRepository<WidgetLinkItemEntity> repo) 
    : IQueryHandler<ListWidgetLinkItemsByWidgetIdQuery, List<WidgetLinkItemEntity>>
{
    public async Task<List<WidgetLinkItemEntity>> HandleAsync(ListWidgetLinkItemsByWidgetIdQuery request, CancellationToken ct)
    {
        var items = await repo.ListAsync(ct);
        return items.Where(x => x.WidgetId == request.WidgetId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Title)
            .ToList();
    }
}