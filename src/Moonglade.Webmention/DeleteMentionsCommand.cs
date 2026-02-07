using LiteBus.Commands.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Webmention;

public record DeleteMentionsCommand(List<Guid> Ids) : ICommand;

public class DeleteMentionsCommandHandler(IRepositoryBase<MentionEntity> repo) : ICommandHandler<DeleteMentionsCommand>
{
    public async Task HandleAsync(DeleteMentionsCommand request, CancellationToken ct)
    {
        if (request.Ids == null || request.Ids.Count == 0)
        {
            return;
        }

        var entities = await repo.ListAsync(new MentionByIdsSpec(request.Ids), ct);
        if (entities.Count != 0)
        {
            await repo.DeleteRangeAsync(entities, ct);
        }
    }
}
