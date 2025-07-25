using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Core;

public record GetStyleSheetQuery(Guid Id) : IQuery<StyleSheetEntity>;

public class GetStyleSheetQueryHandler(MoongladeRepository<StyleSheetEntity> repo) : IQueryHandler<GetStyleSheetQuery, StyleSheetEntity>
{
    public async Task<StyleSheetEntity> HandleAsync(GetStyleSheetQuery request, CancellationToken cancellationToken)
    {
        var result = await repo.GetByIdAsync(request.Id, cancellationToken);
        return result;
    }
}