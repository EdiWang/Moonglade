using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.FriendLink;

public class UpdateLinkCommand : IRequest
{
    public UpdateLinkCommand(Guid id, EditLinkRequest model)
    {
        Id = id;
        Model = model;
    }

    public Guid Id { get; set; }

    public EditLinkRequest Model { get; set; }
}

public class UpdateLinkCommandHandler : IRequestHandler<UpdateLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _friendlinkRepo;

    public UpdateLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo)
    {
        _friendlinkRepo = friendlinkRepo;
    }

    public async Task<Unit> Handle(UpdateLinkCommand request, CancellationToken cancellationToken)
    {
        if (!Uri.IsWellFormedUriString(request.Model.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.Model.LinkUrl)} is not a valid url.");
        }

        var link = await _friendlinkRepo.GetAsync(request.Id);
        if (link is not null)
        {
            link.Title = request.Model.Title;
            link.LinkUrl = Helper.SterilizeLink(request.Model.LinkUrl);

            await _friendlinkRepo.UpdateAsync(link);
        }

        return Unit.Value;
    }
}