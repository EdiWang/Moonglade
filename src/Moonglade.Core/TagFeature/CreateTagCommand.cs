﻿using Moonglade.Data.Generated.Entities;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record CreateTagCommand(string Name) : IRequest<Tag>;

public class CreateTagCommandHandler(IRepository<TagEntity> repo) : IRequestHandler<CreateTagCommand, Tag>
{
    public async Task<Tag> Handle(CreateTagCommand request, CancellationToken ct)
    {
        if (!Tag.ValidateName(request.Name)) return null;

        var normalizedName = Tag.NormalizeName(request.Name, Helper.TagNormalizationDictionary);
        if (await repo.AnyAsync(t => t.NormalizedName == normalizedName, ct))
        {
            return await repo.FirstOrDefaultAsync(new TagSpec(normalizedName), Tag.EntitySelector);
        }

        var newTag = new TagEntity
        {
            DisplayName = request.Name,
            NormalizedName = normalizedName
        };

        var tag = await repo.AddAsync(newTag, ct);

        return new()
        {
            DisplayName = tag.DisplayName,
            NormalizedName = tag.NormalizedName
        };
    }
}