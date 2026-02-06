using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record ListWidgetsQuery : IQuery<List<WidgetEntity>>;

public class ListWidgetsQueryHandler(IRepositoryBase<WidgetEntity> repo) : IQueryHandler<ListWidgetsQuery, List<WidgetEntity>>
{
    public Task<List<WidgetEntity>> HandleAsync(ListWidgetsQuery request, CancellationToken ct) => repo.ListAsync(ct);
}