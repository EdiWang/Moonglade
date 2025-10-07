using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Features.FriendLink;

public record DeleteLinkCommand(Guid Id) : ICommand;

public class DeleteLinkCommandHandler(
    MoongladeRepository<FriendLinkEntity> repo,
    ILogger<DeleteLinkCommandHandler> logger) : ICommandHandler<DeleteLinkCommand>
{
    public async Task HandleAsync(DeleteLinkCommand request, CancellationToken ct)
    {
        var link = await repo.GetByIdAsync(request.Id, ct);
        if (null != link)
        {
            await repo.DeleteAsync(link, ct);
        }

        logger.LogInformation("Deleted a friend link: {Title}", link?.Title);
    }
}