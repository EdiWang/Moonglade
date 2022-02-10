using Microsoft.Extensions.Configuration;
using Moonglade.Data;

namespace Moonglade.Core.TagFeature;

public record UpdateTagCommand(int Id, string Name) : IRequest<OperationCode>;

public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, OperationCode>
{
    private readonly IRepository<TagEntity> _tagRepo;
    private readonly IDictionary<string, string> _tagNormalizationDictionary;

    public UpdateTagCommandHandler(IRepository<TagEntity> tagRepo, IConfiguration configuration)
    {
        _tagRepo = tagRepo;

        _tagNormalizationDictionary =
            configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
    }

    public async Task<OperationCode> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var (id, name) = request;
        var tag = await _tagRepo.GetAsync(id);
        if (null == tag) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = Tag.NormalizeName(name, _tagNormalizationDictionary);
        await _tagRepo.UpdateAsync(tag);

        return OperationCode.Done;
    }
}