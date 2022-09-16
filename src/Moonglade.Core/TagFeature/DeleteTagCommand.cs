using Moonglade.Data;
using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record DeleteTagCommand(int Id) : IRequest<OperationCode>;

public class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand, OperationCode>
{
    private readonly IRepository<TagEntity> _tagRepo;
    private readonly IRepository<PostTagEntity> _postTagRepo;

    public DeleteTagCommandHandler(IRepository<TagEntity> tagRepo, IRepository<PostTagEntity> postTagRepo)
    {
        _tagRepo = tagRepo;
        _postTagRepo = postTagRepo;
    }

    public async Task<OperationCode> Handle(DeleteTagCommand request, CancellationToken ct)
    {
        var exists = await _tagRepo.AnyAsync(c => c.Id == request.Id, ct);
        if (!exists) return OperationCode.ObjectNotFound;

        // 1. Delete Post-Tag Association
        var postTags = await _postTagRepo.ListAsync(new PostTagSpec(request.Id));
        await _postTagRepo.DeleteAsync(postTags, ct);

        // 2. Delte Tag itslef
        await _tagRepo.DeleteAsync(request.Id, ct);

        return OperationCode.Done;
    }
}