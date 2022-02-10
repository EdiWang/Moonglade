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

    public async Task<OperationCode> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var exists = _tagRepo.Any(c => c.Id == request.Id);
        if (!exists) return OperationCode.ObjectNotFound;

        // 1. Delete Post-Tag Association
        var postTags = await _postTagRepo.GetAsync(new PostTagSpec(request.Id));
        await _postTagRepo.DeleteAsync(postTags);

        // 2. Delte Tag itslef
        await _tagRepo.DeleteAsync(request.Id);

        return OperationCode.Done;
    }
}