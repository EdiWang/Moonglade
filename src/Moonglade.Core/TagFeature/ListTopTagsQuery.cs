using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.TagFeature;

public record ListTopTagsQuery(int Top) : IQuery<List<(TagEntity Tag, int PostCount)>>;

public class ListTopTagsQueryHandler(MoongladeRepository<TagEntity> repo) : IQueryHandler<ListTopTagsQuery, List<(TagEntity Tag, int PostCount)>>
{
    public async Task<List<(TagEntity Tag, int PostCount)>> HandleAsync(ListTopTagsQuery request, CancellationToken ct)
    {
        if (!await repo.AnyAsync(ct)) return [];

        var spec = new HotTagSpec(request.Top);
        var tags = await repo.ListAsync(spec, ct);

        return tags;
    }
}