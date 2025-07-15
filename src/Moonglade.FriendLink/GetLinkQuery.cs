using LiteBus.Queries.Abstractions;
using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.FriendLink;

public record GetLinkQuery(Guid Id) : IQuery<FriendLinkEntity>;

public class GetLinkQueryHandler(MoongladeRepository<FriendLinkEntity> repo) : IQueryHandler<GetLinkQuery, FriendLinkEntity>
{
    public async Task<FriendLinkEntity> HandleAsync(GetLinkQuery request, CancellationToken ct) => await repo.GetByIdAsync(request.Id, ct);
}