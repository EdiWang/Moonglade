using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler : AsyncRequestHandler<DeleteLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _friendlinkRepo;

    public DeleteLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo) => _friendlinkRepo = friendlinkRepo;

    protected override Task Handle(DeleteLinkCommand request, CancellationToken cancellationToken) =>
        _friendlinkRepo.DeleteAsync(request.Id, cancellationToken);
}