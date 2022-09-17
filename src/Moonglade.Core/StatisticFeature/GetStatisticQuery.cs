namespace Moonglade.Core.StatisticFeature;

public record GetStatisticQuery(Guid PostId) : IRequest<(int Hits, int Likes)>;

public class GetStatisticQueryHandler : IRequestHandler<GetStatisticQuery, (int Hits, int Likes)>
{
    private readonly IRepository<PostExtensionEntity> _repo;

    public GetStatisticQueryHandler(IRepository<PostExtensionEntity> repo) => _repo = repo;

    public async Task<(int Hits, int Likes)> Handle(GetStatisticQuery request, CancellationToken ct)
    {
        var pp = await _repo.GetAsync(request.PostId, ct);
        return (pp.Hits, pp.Likes);
    }
}