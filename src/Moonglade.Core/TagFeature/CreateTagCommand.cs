using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record CreateTagCommand(string Name) : IRequest;

public class CreateTagCommandHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<CreateTagCommand>
{
    public async Task Handle(CreateTagCommand request, CancellationToken ct)
    {
        var normalizedName = Tag.NormalizeName(request.Name, Helper.TagNormalizationDictionary);

        var existingTag = await repo.FirstOrDefaultAsync(new TagByNormalizedNameSpec(normalizedName), ct);
        if (null != existingTag) return;

        var newTag = new TagEntity
        {
            DisplayName = request.Name,
            NormalizedName = normalizedName
        };

        await repo.AddAsync(newTag, ct);
    }
}