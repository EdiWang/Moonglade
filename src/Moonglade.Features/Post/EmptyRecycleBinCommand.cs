using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record EmptyRecycleBinCommand : ICommand<Guid[]>;

public class EmptyRecycleBinCommandHandler(
    BlogDbContext db,
    ILogger<EmptyRecycleBinCommandHandler> logger
    ) : ICommandHandler<EmptyRecycleBinCommand, Guid[]>
{
    public async Task<Guid[]> HandleAsync(EmptyRecycleBinCommand request, CancellationToken ct)
    {
        var guids = await db.Post
            .Where(p => p.IsDeleted)
            .Select(p => p.Id)
            .ToArrayAsync(ct);

        await db.Post.Where(p => p.IsDeleted).ExecuteDeleteAsync(ct);

        logger.LogInformation("Recycle bin emptied.");

        return guids;
    }
}