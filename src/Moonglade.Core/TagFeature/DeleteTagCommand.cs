using Moonglade.Data;
using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record DeleteTagCommand(int Id) : IRequest<OperationCode>;

public class DeleteTagCommandHandler(
    MoongladeRepository<TagEntity> tagRepo,
    MoongladeRepository<PostTagEntity> postTagRepo)
    : IRequestHandler<DeleteTagCommand, OperationCode>
{
    public async Task<OperationCode> Handle(DeleteTagCommand request, CancellationToken ct)
    {
        var tag = await tagRepo.GetByIdAsync(request.Id, ct);
        if (null == tag) return OperationCode.ObjectNotFound;

        // 1. Delete Post-Tag Association
        var postTags = await postTagRepo.ListAsync(new PostTagByTagIdSpec(request.Id), ct);
        await postTagRepo.DeleteRangeAsync(postTags, ct);

        // 2. Delte Tag itslef
        await tagRepo.DeleteAsync(tag, ct);

        return OperationCode.Done;
    }
}