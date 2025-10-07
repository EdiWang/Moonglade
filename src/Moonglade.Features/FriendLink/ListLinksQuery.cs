using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features.FriendLink;

public record ListLinksQuery : IQuery<List<FriendLinkEntity>>;

public class ListLinksQueryHandler(MoongladeRepository<FriendLinkEntity> repo) : IQueryHandler<ListLinksQuery, List<FriendLinkEntity>>
{
    public Task<List<FriendLinkEntity>> HandleAsync(ListLinksQuery request, CancellationToken ct) => repo.ListAsync(ct);
}