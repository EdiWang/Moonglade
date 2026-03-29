using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Page;

public record GetStyleSheetQuery(Guid Id) : IQuery<StyleSheetEntity>;

public class GetStyleSheetQueryHandler(BlogDbContext db) : IQueryHandler<GetStyleSheetQuery, StyleSheetEntity>
{
    public async Task<StyleSheetEntity> HandleAsync(GetStyleSheetQuery request, CancellationToken cancellationToken)
    {
        var result = await db.StyleSheet.FindAsync([request.Id], cancellationToken);
        return result;
    }
}