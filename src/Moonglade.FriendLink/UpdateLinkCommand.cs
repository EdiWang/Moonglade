using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.FriendLink;

public record UpdateLinkCommand(Guid Id, EditLinkRequest Payload) : IRequest;

public class UpdateLinkCommandHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<UpdateLinkCommand>
{
    public async Task Handle(UpdateLinkCommand request, CancellationToken ct)
    {
        if (!Uri.IsWellFormedUriString(request.Payload.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.Payload.LinkUrl)} is not a valid url.");
        }

        var link = await repo.GetAsync(request.Id, ct);
        if (link is not null)
        {
            link.Title = request.Payload.Title;
            link.LinkUrl = Helper.SterilizeLink(request.Payload.LinkUrl);
            link.Rank = request.Payload.Rank;

            await repo.UpdateAsync(link, ct);
        }
    }
}