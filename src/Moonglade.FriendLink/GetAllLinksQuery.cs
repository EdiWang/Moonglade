using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.FriendLink;

public record GetAllLinksQuery : IRequest<List<FriendLinkEntity>>;

public class GetAllLinksQueryHandler(MoongladeRepository<FriendLinkEntity> repo) : IRequestHandler<GetAllLinksQuery, List<FriendLinkEntity>>
{
    public Task<List<FriendLinkEntity>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        return repo.ListAsync(ct);
    }
}