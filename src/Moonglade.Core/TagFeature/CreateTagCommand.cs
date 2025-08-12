using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record CreateTagCommand(string Name) : ICommand<TagEntity>;

public class CreateTagCommandHandler(
    MoongladeRepository<TagEntity> repo,
    ILogger<CreateTagCommandHandler> logger) : ICommandHandler<CreateTagCommand, TagEntity>
{
    public async Task<TagEntity> HandleAsync(CreateTagCommand request, CancellationToken ct)
    {
        var normalizedName = BlogTagHelper.NormalizeName(request.Name, BlogTagHelper.TagNormalizationDictionary);

        var existingTag = await repo.FirstOrDefaultAsync(new TagByNormalizedNameSpec(normalizedName), ct);
        if (null != existingTag) return existingTag;

        var newTag = new TagEntity
        {
            DisplayName = request.Name,
            NormalizedName = normalizedName
        };

        await repo.AddAsync(newTag, ct);
        logger.LogInformation("Tag created: {TagName}", request.Name);

        return newTag;
    }
}