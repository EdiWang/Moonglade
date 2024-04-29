using Moonglade.Data;

namespace Moonglade.Core.TagFeature;

public record GetTagCountListQuery : IRequest<List<KeyValuePair<TagEntity, int>>>;

public class GetTagCountListQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetTagCountListQuery, List<KeyValuePair<TagEntity, int>>>
{
    public Task<List<KeyValuePair<TagEntity, int>>> Handle(GetTagCountListQuery request, CancellationToken ct) =>
        repo.SelectAsync(t =>
            new KeyValuePair<TagEntity, int>(t, t.Posts.Count), ct);
}