using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Tag;

public record CreateTagCommand(string Name) : ICommand<TagCommandResult>;

public class CreateTagCommandHandler(
    BlogDbContext db,
    ILogger<CreateTagCommandHandler> logger) : ICommandHandler<CreateTagCommand, TagCommandResult>
{
    public async Task<TagCommandResult> HandleAsync(CreateTagCommand request, CancellationToken ct)
    {
        var normalizedName = BlogTagHelper.NormalizeName(request.Name, BlogTagHelper.TagNormalizationDictionary);

        var existingTag = await db.Tag.FirstOrDefaultAsync(t => t.NormalizedName == normalizedName, ct);
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

        await db.Tag.AddAsync(newTag, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Tag created: {TagName}", request.Name);

        return new TagCommandResult
        {
            Id = newTag.Id,
            DisplayName = newTag.DisplayName,
            NormalizedName = newTag.NormalizedName
        };
    }
}