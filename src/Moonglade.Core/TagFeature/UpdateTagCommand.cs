using Moonglade.Data;
using Moonglade.Utils;

namespace Moonglade.Core.TagFeature;

public record UpdateTagCommand(int Id, string Name) : IRequest<OperationCode>;

public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, OperationCode>
{
    private readonly IRepository<TagEntity> _repo;

    public UpdateTagCommandHandler(IRepository<TagEntity> repo) => _repo = repo;

    public async Task<OperationCode> Handle(UpdateTagCommand request, CancellationToken ct)
    {
        var (id, name) = request;
        var tag = await _repo.GetAsync(id, ct);
        if (null == tag) return OperationCode.ObjectNotFound;

        tag.DisplayName = name;
        tag.NormalizedName = Tag.NormalizeName(name, Helper.TagNormalizationDictionary);
        await _repo.UpdateAsync(tag, ct);

        return OperationCode.Done;
    }
}