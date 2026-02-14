using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Features.Tag;

public record CreateTagCommand(string Name) : ICommand<TagCommandResult>;

public class CreateTagCommandHandler(
    IRepositoryBase<TagEntity> repo,
    ILogger<CreateTagCommandHandler> logger) : ICommandHandler<CreateTagCommand, TagCommandResult>
{
    public async Task<TagCommandResult> HandleAsync(CreateTagCommand request, CancellationToken ct)
    {
        var normalizedName = BlogTagHelper.NormalizeName(request.Name, BlogTagHelper.TagNormalizationDictionary);

        var existingTag = await repo.FirstOrDefaultAsync(new TagByNormalizedNameSpec(normalizedName), ct);
        if (null != existingTag) return new TagCommandResult
        {
            Id = existingTag.Id,
            DisplayName = existingTag.DisplayName,
            NormalizedName = existingTag.NormalizedName
        };

        var newTag = new TagEntity
        {
            DisplayName = request.Name,
            NormalizedName = normalizedName
        };

        await repo.AddAsync(newTag, ct);
        logger.LogInformation("Tag created: {TagName}", request.Name);

        return new TagCommandResult
        {
            Id = newTag.Id,
            DisplayName = newTag.DisplayName,
            NormalizedName = newTag.NormalizedName
        };
    }
}