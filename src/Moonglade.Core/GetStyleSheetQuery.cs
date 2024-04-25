using Moonglade.Data;

namespace Moonglade.Core;

public record GetStyleSheetQuery(Guid Id) : IRequest<StyleSheetEntity>;

public class GetStyleSheetQueryHandler(MoongladeRepository<StyleSheetEntity> repo) : IRequestHandler<GetStyleSheetQuery, StyleSheetEntity>
{
    public async Task<StyleSheetEntity> Handle(GetStyleSheetQuery request, CancellationToken cancellationToken)
    {
        var result = await repo.GetByIdAsync(request.Id, cancellationToken);
        return result;
    }
}