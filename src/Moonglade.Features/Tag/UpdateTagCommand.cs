using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Utils;

namespace Moonglade.Features.Tag;

public record UpdateTagCommand(int Id, string Name) : ICommand<OperationCode>;

public class UpdateTagCommandHandler(
    BlogDbContext db,
    ILogger<UpdateTagCommandHandler> logger) : ICommandHandler<UpdateTagCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateTagCommand request, CancellationToken ct)
    {
        var (id, name) = request;
        var tag = await db.Tag.FindAsync([id], ct);
        if (tag == null) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = BlogTagHelper.NormalizeName(name, BlogTagHelper.TagNormalizationDictionary);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated tag: {TagId}", request.Id);
        return OperationCode.Done;
    }
}