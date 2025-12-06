using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record GetWidgetContentQuery(Guid Id) : IQuery<WidgetContentEntity>;

public class GetWidgetContentQueryHandler(MoongladeRepository<WidgetContentEntity> repo) : IQueryHandler<GetWidgetContentQuery, WidgetContentEntity>
{
    public Task<WidgetContentEntity> HandleAsync(GetWidgetContentQuery request, CancellationToken ct) => repo.GetByIdAsync(request.Id, ct);
}