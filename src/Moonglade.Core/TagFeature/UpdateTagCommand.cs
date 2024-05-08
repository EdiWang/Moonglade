using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record UpdateTagCommand(int Id, string Name) : IRequest<OperationCode>;

public class UpdateTagCommandHandler(
    MoongladeRepository<TagEntity> repo,
    ILogger<UpdateTagCommandHandler> logger) : IRequestHandler<UpdateTagCommand, OperationCode>
{
    public async Task<OperationCode> Handle(UpdateTagCommand request, CancellationToken ct)
    {
        var (id, name) = request;
        var tag = await repo.GetByIdAsync(id, ct);
        if (null == tag) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = Helper.NormalizeName(name, Helper.TagNormalizationDictionary);
        await repo.UpdateAsync(tag, ct);

        logger.LogInformation("Updated tag: {TagId}", request.Id);
        return OperationCode.Done;
    }
}