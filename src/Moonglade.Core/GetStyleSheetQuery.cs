namespace Moonglade.Core;

public record GetStyleSheetQuery(Guid Id) : IRequest<StyleSheetEntity>;

public class GetStyleSheetQueryHandler(IRepository<StyleSheetEntity> repo) : IRequestHandler<GetStyleSheetQuery, StyleSheetEntity>
{
    public async Task<StyleSheetEntity> Handle(GetStyleSheetQuery request, CancellationToken cancellationToken)
    {
        var result = await repo.GetAsync(request.Id, cancellationToken);
        return result;
    }
}