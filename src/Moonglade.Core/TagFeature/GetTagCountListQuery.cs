namespace Moonglade.Core.TagFeature;

public record GetTagCountListQuery : IRequest<IReadOnlyList<KeyValuePair<Tag, int>>>;

public class GetTagCountListQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetTagCountListQuery, IReadOnlyList<KeyValuePair<Tag, int>>>
{
    public Task<IReadOnlyList<KeyValuePair<Tag, int>>> Handle(GetTagCountListQuery request, CancellationToken ct) =>
        repo.SelectAsync(t =>
            new KeyValuePair<Tag, int>(new()
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            }, t.Posts.Count), ct);
}