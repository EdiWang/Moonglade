using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record GetAllLinksQuery : IRequest<IReadOnlyList<Link>>;

public class GetAllLinksQueryHandler : IRequestHandler<GetAllLinksQuery, IReadOnlyList<Link>>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public GetAllLinksQueryHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<Link>> Handle(GetAllLinksQuery request, CancellationToken ct)
    {
        var data = _repo.SelectAsync(f => new Link
        {
            Id = f.Id,
            LinkUrl = f.LinkUrl,
            Title = f.Title
        });

        return data;
    }
}