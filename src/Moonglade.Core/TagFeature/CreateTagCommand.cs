using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record CreateTagCommand(string Name) : IRequest<Tag>;

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, Tag>
{
    private readonly IRepository<TagEntity> _repo;

    public CreateTagCommandHandler(IRepository<TagEntity> repo) => _repo = repo;

    public async Task<Tag> Handle(CreateTagCommand request, CancellationToken ct)
    {
        if (!Tag.ValidateName(request.Name)) return null;

        var normalizedName = Tag.NormalizeName(request.Name, Helper.TagNormalizationDictionary);
        if (await _repo.AnyAsync(t => t.NormalizedName == normalizedName, ct))
        {
            return await _repo.FirstOrDefaultAsync(new TagSpec(normalizedName), Tag.EntitySelector);
        }

        var newTag = new TagEntity
        {
            DisplayName = request.Name,
            NormalizedName = normalizedName
        };

        var tag = await _repo.AddAsync(newTag, ct);

        return new()
        {
            DisplayName = tag.DisplayName,
            NormalizedName = tag.NormalizedName
        };
    }
}