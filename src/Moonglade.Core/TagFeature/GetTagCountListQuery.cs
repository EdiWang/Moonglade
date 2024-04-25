using Moonglade.Data;

namespace Moonglade.Core.TagFeature;

public record GetTagCountListQuery : IRequest<List<KeyValuePair<Tag, int>>>;

public class GetTagCountListQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetTagCountListQuery, List<KeyValuePair<Tag, int>>>
{
    public Task<List<KeyValuePair<Tag, int>>> Handle(GetTagCountListQuery request, CancellationToken ct) =>
        repo.SelectAsync(t =>
            new KeyValuePair<Tag, int>(new()
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            }, t.Posts.Count), ct);
}