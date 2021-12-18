using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public class DeleteLinkCommand : IRequest
{
    public DeleteLinkCommand(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}

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