using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.FriendLink;

public class UpdateLinkCommand : AddLinkCommand
{
    public Guid Id { get; set; }
}

public class UpdateLinkCommandHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<UpdateLinkCommand>
{
    public async Task Handle(UpdateLinkCommand request, CancellationToken ct)
    {
        if (!Uri.IsWellFormedUriString(request.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.LinkUrl)} is not a valid url.");
        }

        var link = await repo.GetAsync(request.Id, ct);
        if (link is not null)
        {
            link.Title = request.Title;
            link.LinkUrl = Helper.SterilizeLink(request.LinkUrl);
            link.Rank = request.Rank;

            await repo.UpdateAsync(link, ct);
        }
    }
}