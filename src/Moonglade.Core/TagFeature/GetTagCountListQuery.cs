namespace Moonglade.Core.TagFeature;

public record GetTagCountListQuery : IRequest<IReadOnlyList<KeyValuePair<Tag, int>>>;

public class GetTagCountListQueryHandler : IRequestHandler<GetTagCountListQuery, IReadOnlyList<KeyValuePair<Tag, int>>>
{
    private readonly IRepository<TagEntity> _repo;

    public GetTagCountListQueryHandler(IRepository<TagEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<KeyValuePair<Tag, int>>> Handle(GetTagCountListQuery request, CancellationToken ct) =>
        _repo.SelectAsync(t =>
            new KeyValuePair<Tag, int>(new()
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            }, t.Posts.Count), ct);
}