using Moonglade.Data;

namespace Moonglade.Core.PageFeature;

public record GetPageByIdQuery(Guid Id) : IRequest<PageEntity>;

public class GetPageByIdQueryHandler(MoongladeRepository<PageEntity> repo) : IRequestHandler<GetPageByIdQuery, PageEntity>
{
    public Task<PageEntity> Handle(GetPageByIdQuery request, CancellationToken ct) => repo.GetByIdAsync(request.Id, ct);
}