using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.FriendLink;

public class UpdateLinkCommand : AddLinkCommand
{
    public Guid Id { get; set; }
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
        if (!Uri.IsWellFormedUriString(request.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.LinkUrl)} is not a valid url.");
        }

        var link = await _friendlinkRepo.GetAsync(request.Id);
        if (link is not null)
        {
            link.Title = request.Title;
            link.LinkUrl = Helper.SterilizeLink(request.LinkUrl);

            await _friendlinkRepo.UpdateAsync(link);
        }

        return Unit.Value;
    }
}