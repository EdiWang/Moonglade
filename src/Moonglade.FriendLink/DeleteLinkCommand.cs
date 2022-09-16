using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler : AsyncRequestHandler<DeleteLinkCommand>
{
    private readonly IRepository<FriendLinkEntity> _repo;

    public DeleteLinkCommandHandler(IRepository<FriendLinkEntity> repo) => _repo = repo;

    protected override Task Handle(DeleteLinkCommand request, CancellationToken ct) =>
        _repo.DeleteAsync(request.Id, ct);
}