using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Mention.Common;

public record DeleteMentionCommand(Guid Id) : ICommand;

public class DeletePingbackCommandHandler(MoongladeRepository<MentionEntity> repo) : ICommandHandler<DeleteMentionCommand>
{
    public async Task HandleAsync(DeleteMentionCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity != null)
        {
            await repo.DeleteAsync(entity, ct);
        }
    }
}