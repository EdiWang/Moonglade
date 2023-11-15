using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record GetAllLinksQuery : IRequest<IReadOnlyList<FriendLinkEntity>>;

public class GetAllLinksQueryHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<GetAllLinksQuery, IReadOnlyList<FriendLinkEntity>>
{
    public Task<IReadOnlyList<FriendLinkEntity>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        return repo.ListAsync(ct);
    }
}