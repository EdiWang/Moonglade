using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Webmention;

public record ListMentionsQuery : IQuery<List<MentionEntity>>;

public class ListMentionsQueryHandler(MoongladeRepository<MentionEntity> repo) :
    IQueryHandler<ListMentionsQuery, List<MentionEntity>>
{
    public Task<List<MentionEntity>> HandleAsync(ListMentionsQuery request, CancellationToken ct) =>
        repo.ListAsync(new MentionReadOnlySpec(), ct);
}