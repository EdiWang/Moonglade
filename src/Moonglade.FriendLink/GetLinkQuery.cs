using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record GetLinkQuery(Guid Id) : IRequest<FriendLinkEntity>;

public class GetLinkQueryHandler : IRequestHandler<GetLinkQuery, FriendLinkEntity>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public GetLinkQueryHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public async Task<FriendLinkEntity> Handle(GetLinkQuery request, CancellationToken ct)
    {
        return await _repo.GetAsync(request.Id, ct);
    }
}