using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.TagFeature;

public record GetTagNamesQuery : IQuery<List<string>>;

public class GetTagNamesQueryHandler(MoongladeRepository<TagEntity> repo) : IQueryHandler<GetTagNamesQuery, List<string>>
{
    public Task<List<string>> HandleAsync(GetTagNamesQuery request, CancellationToken ct) =>
        repo.ListAsync(new TagDisplayNameNameSpec(), ct);
}