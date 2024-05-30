using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Mention.Common;

public record GetMentionsQuery : IRequest<List<MentionEntity>>;

public class GetMentionsQueryHandler(MoongladeRepository<MentionEntity> repo) :
    IRequestHandler<GetMentionsQuery, List<MentionEntity>>
{
    public Task<List<MentionEntity>> Handle(GetMentionsQuery request, CancellationToken ct) =>
        repo.ListAsync(new MentionReadOnlySpec(), ct);
}