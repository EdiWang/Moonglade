namespace Moonglade.Core.StatisticFeature;

public record GetStatisticQuery(Guid PostId) : IRequest<int>;

public class GetStatisticQueryHandler : IRequestHandler<GetStatisticQuery, int>
{
    private readonly IRepository<PostExtensionEntity> _repo;

    public GetStatisticQueryHandler(IRepository<PostExtensionEntity> repo) => _repo = repo;

    public async Task<int> Handle(GetStatisticQuery request, CancellationToken ct)
    {
        var pp = await _repo.GetAsync(request.PostId, ct);
        return pp.Hits;
    }
}