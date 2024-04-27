using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler(MoongladeRepository<FriendLinkEntity> repo) : IRequestHandler<DeleteLinkCommand>
{
    public async Task Handle(DeleteLinkCommand request, CancellationToken ct)
    {
        var link = await repo.GetByIdAsync(request.Id, ct);
        if (null != link)
        {
            await repo.DeleteAsync(link, ct);
        }
    }
}