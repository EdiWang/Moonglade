using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record GetAllLinksQuery : IRequest<List<FriendLinkEntity>>;

public class GetAllLinksQueryHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<GetAllLinksQuery, List<FriendLinkEntity>>
{
    public Task<List<FriendLinkEntity>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        return repo.ListAsync(ct);
    }
}