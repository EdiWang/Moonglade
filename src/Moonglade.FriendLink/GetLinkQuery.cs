using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.FriendLink;

public record GetLinkQuery(Guid Id) : IRequest<Link>;

public class GetLinkQueryHandler : IRequestHandler<GetLinkQuery, Link>
{
    private readonly IRepository<FriendLinkEntity> _friendlinkRepo;

    public GetLinkQueryHandler(IRepository<FriendLinkEntity> friendlinkRepo)
    {
        _friendlinkRepo = friendlinkRepo;
    }

    public Task<Link> Handle(GetLinkQuery request, CancellationToken cancellationToken)
    {
        var item = _friendlinkRepo.SelectFirstOrDefaultAsync(
            new FriendLinkSpec(request.Id), f => new Link
            {
                Id = f.Id,
                LinkUrl = f.LinkUrl,
                Title = f.Title
            });
        return item;
    }
}