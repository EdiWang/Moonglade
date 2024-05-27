using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Pingback;

public record DeleteMentionCommand(Guid Id) : IRequest;

public class DeletePingbackCommandHandler(MoongladeRepository<MentionEntity> repo) : IRequestHandler<DeleteMentionCommand>
{
    public async Task Handle(DeleteMentionCommand request, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(request.Id, ct);
        if (entity != null)
        {
            await repo.DeleteAsync(entity, ct);
        }
    }
}