using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record GetLinkQuery(Guid Id) : IRequest<FriendLinkEntity>;

public class GetLinkQueryHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<GetLinkQuery, FriendLinkEntity>
{
    public async Task<FriendLinkEntity> Handle(GetLinkQuery request, CancellationToken ct)
    {
        return await repo.GetAsync(request.Id, ct);
    }
}