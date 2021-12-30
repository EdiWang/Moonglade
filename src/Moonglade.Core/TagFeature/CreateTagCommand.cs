using MediatR;
using Microsoft.Extensions.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public class CreateTagCommand : IRequest<Tag>
{
    public CreateTagCommand(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, Tag>
{
    private readonly IRepository<TagEntity> _tagRepo;
    private readonly IDictionary<string, string> _tagNormalizationDictionary;

    public CreateTagCommandHandler(IRepository<TagEntity> tagRepo, IConfiguration configuration)
    {
        _tagRepo = tagRepo;
        _tagNormalizationDictionary =
            configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
    }

    public async Task<Tag> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        if (!Tag.ValidateName(request.Name)) return null;

        var normalizedName = Tag.NormalizeName(request.Name, _tagNormalizationDictionary);
        if (_tagRepo.Any(t => t.NormalizedName == normalizedName))
        {
            return _tagRepo.SelectFirstOrDefault(new TagSpec(normalizedName), Tag.EntitySelector);
        }

        var newTag = new TagEntity
        {
            DisplayName = request.Name,
            NormalizedName = normalizedName
        };

        var tag = await _tagRepo.AddAsync(newTag);

        return new()
        {
            DisplayName = newTag.DisplayName,
            NormalizedName = newTag.NormalizedName
        };
    }
}