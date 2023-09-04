namespace Moonglade.Core;

public record GetStyleSheetQuery(Guid Id) : IRequest<StyleSheetEntity>;

public class GetStyleSheetQueryHandler : IRequestHandler<GetStyleSheetQuery, StyleSheetEntity>
{
    private readonly IRepository<StyleSheetEntity> _repo;

    public GetStyleSheetQueryHandler(IRepository<StyleSheetEntity> repo) => _repo = repo;

    public async Task<StyleSheetEntity> Handle(GetStyleSheetQuery request, CancellationToken cancellationToken)
    {
        var result = await _repo.GetAsync(request.Id, cancellationToken);
        return result;
    }
}