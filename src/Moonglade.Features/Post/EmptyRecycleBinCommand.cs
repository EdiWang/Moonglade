using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record EmptyRecycleBinCommand : ICommand<Guid[]>;

public class EmptyRecycleBinCommandHandler(
    BlogDbContext db,
    ILogger<EmptyRecycleBinCommandHandler> logger,
    IEventMediator eventMediator
    ) : ICommandHandler<EmptyRecycleBinCommand, Guid[]>
{
    public async Task<Guid[]> HandleAsync(EmptyRecycleBinCommand request, CancellationToken ct)
    {
        var guids = await db.Post
            .Where(p => p.IsDeleted)
            .Select(p => p.Id)
            .ToArrayAsync(ct);

        if (await db.Post.Where(p => p.IsDeleted).ExecuteDeleteAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Recycle bin emptied.");

        return guids;
    }
}