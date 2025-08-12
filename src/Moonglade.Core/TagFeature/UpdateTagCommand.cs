using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record UpdateTagCommand(int Id, string Name) : ICommand<OperationCode>;

public class UpdateTagCommandHandler(
    MoongladeRepository<TagEntity> repo,
    ILogger<UpdateTagCommandHandler> logger) : ICommandHandler<UpdateTagCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateTagCommand request, CancellationToken ct)
    {
        var (id, name) = request;
        var tag = await repo.GetByIdAsync(id, ct);
        if (null == tag) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = BlogTagHelper.NormalizeName(name, BlogTagHelper.TagNormalizationDictionary);
        await repo.UpdateAsync(tag, ct);

        logger.LogInformation("Updated tag: {TagId}", request.Id);
        return OperationCode.Done;
    }
}