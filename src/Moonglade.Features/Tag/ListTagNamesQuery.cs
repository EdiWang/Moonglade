using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Tag;

public record ListTagNamesQuery : IQuery<List<string>>;

public class ListTagNamesQueryHandler(MoongladeRepository<TagEntity> repo) : IQueryHandler<ListTagNamesQuery, List<string>>
{
    public Task<List<string>> HandleAsync(ListTagNamesQuery request, CancellationToken ct) =>
        repo.ListAsync(new TagDisplayNameNameSpec(), ct);
}