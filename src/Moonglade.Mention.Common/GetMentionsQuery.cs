using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Mention.Common;

public record GetMentionsQuery : IQuery<List<MentionEntity>>;

public class GetMentionsQueryHandler(MoongladeRepository<MentionEntity> repo) :
    IQueryHandler<GetMentionsQuery, List<MentionEntity>>
{
    public Task<List<MentionEntity>> HandleAsync(GetMentionsQuery request, CancellationToken ct) =>
        repo.ListAsync(new MentionReadOnlySpec(), ct);
}