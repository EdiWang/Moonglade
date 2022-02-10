namespace Moonglade.Core.StatisticFeature;

public record GetStatisticQuery(Guid PostId) : IRequest<(int Hits, int Likes)>;

public class GetStatisticQueryHandler : IRequestHandler<GetStatisticQuery, (int Hits, int Likes)>
{
    private readonly IRepository<PostExtensionEntity> _postExtensionRepo;

    public GetStatisticQueryHandler(IRepository<PostExtensionEntity> postExtensionRepo)
    {
        _postExtensionRepo = postExtensionRepo;
    }

    public async Task<(int Hits, int Likes)> Handle(GetStatisticQuery request, CancellationToken cancellationToken)
    {
        var pp = await _postExtensionRepo.GetAsync(request.PostId);
        return (pp.Hits, pp.Likes);
    }
}