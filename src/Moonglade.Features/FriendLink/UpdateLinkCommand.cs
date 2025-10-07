using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Utils;

namespace Moonglade.Features.FriendLink;

public record UpdateLinkCommand(Guid Id, EditLinkRequest Payload) : ICommand;

public class UpdateLinkCommandHandler(
    MoongladeRepository<FriendLinkEntity> repo,
    ILogger<UpdateLinkCommandHandler> logger) : ICommandHandler<UpdateLinkCommand>
{
    public async Task HandleAsync(UpdateLinkCommand request, CancellationToken ct)
    {
        if (!Uri.IsWellFormedUriString(request.Payload.LinkUrl, UriKind.Absolute))
        {
            throw new InvalidOperationException($"{nameof(request.Payload.LinkUrl)} is not a valid url.");
        }

        var link = await repo.GetByIdAsync(request.Id, ct);
        if (link is not null)
        {
            link.Title = request.Payload.Title;
            link.LinkUrl = SecurityHelper.SterilizeLink(request.Payload.LinkUrl);
            link.Rank = request.Payload.Rank;

            await repo.UpdateAsync(link, ct);
        }

        logger.LogInformation("Updated link: {LinkId}", request.Id);
    }
}