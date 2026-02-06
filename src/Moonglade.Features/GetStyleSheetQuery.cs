using LiteBus.Queries.Abstractions;

namespace Moonglade.Features;

public record GetStyleSheetQuery(Guid Id) : IQuery<StyleSheetEntity>;

public class GetStyleSheetQueryHandler(IRepositoryBase<StyleSheetEntity> repo) : IQueryHandler<GetStyleSheetQuery, StyleSheetEntity>
{
    public async Task<StyleSheetEntity> HandleAsync(GetStyleSheetQuery request, CancellationToken cancellationToken)
    {
        var result = await repo.GetByIdAsync(request.Id, cancellationToken);
        return result;
    }
}