using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.TagFeature;

public record GetHotTagsQuery(int Top) : IRequest<List<(TagEntity Tag, int PostCount)>>;

public class GetHotTagsQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetHotTagsQuery, List<(TagEntity Tag, int PostCount)>>
{
    public async Task<List<(TagEntity Tag, int PostCount)>> Handle(GetHotTagsQuery request, CancellationToken ct)
    {
        if (!await repo.AnyAsync(ct)) return [];

        var spec = new HotTagSpec(request.Top);
        var tags = await repo.ListAsync(spec, ct);

        return tags;
    }
}