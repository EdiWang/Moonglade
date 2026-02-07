using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;

namespace Moonglade.Widgets;

public record GetWidgetQuery(Guid Id) : IQuery<WidgetEntity>;

public class GetWidgetQueryHandler(IRepositoryBase<WidgetEntity> repo) : IQueryHandler<GetWidgetQuery, WidgetEntity>
{
    public Task<WidgetEntity> HandleAsync(GetWidgetQuery request, CancellationToken ct) => repo.GetByIdAsync(request.Id, ct);
}