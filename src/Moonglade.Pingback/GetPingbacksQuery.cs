using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Pingback;

public record GetPingbacksQuery : IRequest<List<MentionEntity>>;

public class GetPingbacksQueryHandler(MoongladeRepository<MentionEntity> repo) :
    IRequestHandler<GetPingbacksQuery, List<MentionEntity>>
{
    public Task<List<MentionEntity>> Handle(GetPingbacksQuery request, CancellationToken ct) =>
        repo.ListAsync(new PingbackReadOnlySpec(), ct);
}