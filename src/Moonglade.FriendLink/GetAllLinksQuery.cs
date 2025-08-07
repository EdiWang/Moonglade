using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.FriendLink;

public record GetAllLinksQuery : IQuery<List<FriendLinkEntity>>;

public class GetAllLinksQueryHandler(MoongladeRepository<FriendLinkEntity> repo) : IQueryHandler<GetAllLinksQuery, List<FriendLinkEntity>>
{
    public Task<List<FriendLinkEntity>> HandleAsync(GetAllLinksQuery request, CancellationToken ct) => repo.ListAsync(ct);
}