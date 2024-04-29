using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.TagFeature;

public record GetHotTagsQuery(int Top) : IRequest<List<KeyValuePair<TagEntity, int>>>;

public class GetHotTagsQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetHotTagsQuery, List<KeyValuePair<TagEntity, int>>>
{
    public async Task<List<KeyValuePair<TagEntity, int>>> Handle(GetHotTagsQuery request, CancellationToken ct)
    {
        if (!await repo.AnyAsync(ct)) return [];

        var spec = new TagSpec(request.Top);
        var tags = await repo.SelectAsync(spec, t =>
            new KeyValuePair<TagEntity, int>(t, t.Posts.Count), ct);

        return tags;
    }
}