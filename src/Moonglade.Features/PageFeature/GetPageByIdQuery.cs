using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features.PageFeature;

public record GetPageByIdQuery(Guid Id) : IQuery<PageEntity>;

public class GetPageByIdQueryHandler(MoongladeRepository<PageEntity> repo) : IQueryHandler<GetPageByIdQuery, PageEntity>
{
    public Task<PageEntity> HandleAsync(GetPageByIdQuery request, CancellationToken ct) => repo.GetByIdAsync(request.Id, ct);
}