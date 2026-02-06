using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Page;

public record GetPageByIdQuery(Guid Id) : IQuery<PageEntity>;

public class GetPageByIdQueryHandler(IRepositoryBase<PageEntity> repo) : IQueryHandler<GetPageByIdQuery, PageEntity>
{
    public Task<PageEntity> HandleAsync(GetPageByIdQuery request, CancellationToken ct) => repo.GetByIdAsync(request.Id, ct);
}