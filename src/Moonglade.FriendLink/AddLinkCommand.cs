using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.FriendLink;

public class AddLinkCommand : IRequest
{
    public AddLinkCommand(EditLinkRequest payload)
    {
        Payload = payload;
    }

    public EditLinkRequest Payload { get; set; }
}

public class AddLinkCommandHandler : IRequestHandler<AddLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _friendlinkRepo;

    public AddLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo)
    {
        _friendlinkRepo = friendlinkRepo;
    }

    public async Task<Unit> Handle(AddLinkCommand request, CancellationToken cancellationToken)
    {
        if (!Uri.IsWellFormedUriString(request.Payload.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.Payload.LinkUrl)} is not a valid url.");
        }

        var link = new FriendLinkEntity
        {
            Id = Guid.NewGuid(),
            LinkUrl = Helper.SterilizeLink(request.Payload.LinkUrl),
            Title = request.Payload.Title
        };

        await _friendlinkRepo.AddAsync(link);

        return Unit.Value;
    }
}