using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Tag;

public record GetTagCountListQuery : IQuery<List<(TagEntity Tag, int PostCount)>>;

public class GetTagCountListQueryHandler(MoongladeRepository<TagEntity> repo) : IQueryHandler<GetTagCountListQuery, List<(TagEntity Tag, int PostCount)>>
{
    public Task<List<(TagEntity Tag, int PostCount)>> HandleAsync(GetTagCountListQuery request, CancellationToken ct) =>
        repo.ListAsync(new TagCloudSpec(), ct);
}