using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Page;

public record GetPageByIdQuery(Guid Id) : IQuery<PageEntity>;

public class GetPageByIdQueryHandler(BlogDbContext db) : IQueryHandler<GetPageByIdQuery, PageEntity>
{
    public Task<PageEntity> HandleAsync(GetPageByIdQuery request, CancellationToken ct) => db.BlogPage.FindAsync([request.Id], ct).AsTask();
}