using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public record EmptyRecycleBinCommand : ICommand<Guid[]>;

public class EmptyRecycleBinCommandHandler(
    MoongladeRepository<PostEntity> repo,
    ILogger<EmptyRecycleBinCommandHandler> logger
    ) : ICommandHandler<EmptyRecycleBinCommand, Guid[]>
{
    public async Task<Guid[]> HandleAsync(EmptyRecycleBinCommand request, CancellationToken ct)
    {
        var spec = new PostByDeletionFlagSpec(true);
        var posts = await repo.ListAsync(spec, ct);
        await repo.DeleteRangeAsync(posts, ct);

        logger.LogInformation("Recycle bin emptied.");

        var guids = posts.Select(p => p.Id).ToArray();
        return guids;
    }
}