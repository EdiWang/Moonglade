using MediatR;
using Moonglade.Data;
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
    private readonly IBlogAudit _audit;

    public AddLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo, IBlogAudit audit)
    {
        _friendlinkRepo = friendlinkRepo;
        _audit = audit;
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
        await _audit.AddEntry(BlogEventType.Content, BlogEventId.FriendLinkCreated, "FriendLink created.");

        return Unit.Value;
    }
}