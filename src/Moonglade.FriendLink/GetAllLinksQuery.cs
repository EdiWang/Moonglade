using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record GetAllLinksQuery : IRequest<IReadOnlyList<FriendLinkEntity>>;

public class GetAllLinksQueryHandler : IRequestHandler<GetAllLinksQuery, IReadOnlyList<FriendLinkEntity>>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public GetAllLinksQueryHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<FriendLinkEntity>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        return _repo.ListAsync(ct);
    }
}