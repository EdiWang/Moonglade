using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Tag;

public record DeleteTagCommand(int Id) : ICommand<OperationCode>;

public class DeleteTagCommandHandler(
    IRepositoryBase<TagEntity> tagRepo,
    IRepositoryBase<PostTagEntity> postTagRepo,
    ILogger<DeleteTagCommandHandler> logger)
    : ICommandHandler<DeleteTagCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteTagCommand request, CancellationToken ct)
    {
        var tag = await tagRepo.GetByIdAsync(request.Id, ct);
        if (null == tag) return OperationCode.ObjectNotFound;

        // 1. Delete Post-Tag Association
        var postTags = await postTagRepo.ListAsync(new PostTagByTagIdSpec(request.Id), ct);
        await postTagRepo.DeleteRangeAsync(postTags, ct);

        // 2. Delte Tag itslef
        await tagRepo.DeleteAsync(tag, ct);

        logger.LogInformation("Deleted tag: {TagId}", request.Id);
        return OperationCode.Done;
    }
}