namespace Moonglade.Core.TagFeature;

public record GetTagNamesQuery : IRequest<IReadOnlyList<string>>;

public class GetTagNamesQueryHandler : IRequestHandler<GetTagNamesQuery, IReadOnlyList<string>>
{
    private readonly IRepository<TagEntity> _repo;

    public GetTagNamesQueryHandler(IRepository<TagEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<string>> Handle(GetTagNamesQuery request, CancellationToken ct) => _repo.SelectAsync(t => t.DisplayName);
}