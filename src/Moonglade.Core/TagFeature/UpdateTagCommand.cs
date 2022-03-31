using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record UpdateTagCommand(int Id, string Name) : IRequest<OperationCode>;

public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, OperationCode>
{
    private readonly IRepository<TagEntity> _tagRepo;

    public UpdateTagCommandHandler(IRepository<TagEntity> tagRepo) => _tagRepo = tagRepo;

    public async Task<OperationCode> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var (id, name) = request;
        var tag = await _tagRepo.GetAsync(id);
        if (null == tag) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = Tag.NormalizeName(name, Helper.TagNormalizationDictionary);
        await _tagRepo.UpdateAsync(tag);

        return OperationCode.Done;
    }
}