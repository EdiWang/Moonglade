using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Features.Post;
using Moonglade.Utils;

namespace Moonglade.Features.Tag;

public record UpdateTagCommand(int Id, string Name) : ICommand<OperationCode>;

public class UpdateTagCommandHandler(
    BlogDbContext db,
    ILogger<UpdateTagCommandHandler> logger,
    IEventMediator eventMediator) : ICommandHandler<UpdateTagCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateTagCommand request, CancellationToken ct)
    {
        var (id, name) = request;
        var tag = await db.Tag.FindAsync([id], ct);
        if (tag == null) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = BlogTagHelper.NormalizeName(name, BlogTagHelper.TagNormalizationDictionary);
        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

        logger.LogInformation("Updated tag: {TagId}", request.Id);
        return OperationCode.Done;
    }
}
