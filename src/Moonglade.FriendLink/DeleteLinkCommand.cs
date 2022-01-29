using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler : IRequestHandler<DeleteLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _friendlinkRepo;

    public DeleteLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo)
    {
        _friendlinkRepo = friendlinkRepo;
    }

    public async Task<Unit> Handle(DeleteLinkCommand request, CancellationToken cancellationToken)
    {
        await _friendlinkRepo.DeleteAsync(request.Id);
        return Unit.Value;
    }
}