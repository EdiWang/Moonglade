using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.FriendLink;

public record GetLinkQuery(Guid Id) : IRequest<Link>;

public class GetLinkQueryHandler : IRequestHandler<GetLinkQuery, Link>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public GetLinkQueryHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task<Link> Handle(GetLinkQuery request, CancellationToken ct)
    {
        return _repo.FirstOrDefaultAsync(
             new FriendLinkSpec(request.Id), f => new Link
             {
                 Id = f.Id,
                 LinkUrl = f.LinkUrl,
                 Title = f.Title
             });
    }
}