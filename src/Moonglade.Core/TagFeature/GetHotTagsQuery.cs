using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetHotTagsQuery(int Top) : IRequest<IReadOnlyList<KeyValuePair<Tag, int>>>;

public class GetHotTagsQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetHotTagsQuery, IReadOnlyList<KeyValuePair<Tag, int>>>
{
    public async Task<IReadOnlyList<KeyValuePair<Tag, int>>> Handle(GetHotTagsQuery request, CancellationToken ct)
    {
        if (!await repo.AnyAsync(ct: ct)) return new List<KeyValuePair<Tag, int>>();

        var spec = new TagSpec(request.Top);
        var tags = await repo.SelectAsync(spec, t =>
            new KeyValuePair<Tag, int>(new()
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            }, t.Posts.Count), ct);

        return tags;
    }
}