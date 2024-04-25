using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.FriendLink;

public record GetLinkQuery(Guid Id) : IRequest<FriendLinkEntity>;

public class GetLinkQueryHandler(MoongladeRepository<FriendLinkEntity> repo) : IRequestHandler<GetLinkQuery, FriendLinkEntity>
{
    public async Task<FriendLinkEntity> Handle(GetLinkQuery request, CancellationToken ct) => await repo.GetByIdAsync(request.Id, ct);
}