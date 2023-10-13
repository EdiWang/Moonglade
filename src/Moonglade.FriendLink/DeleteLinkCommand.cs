using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler(IRepository<FriendLinkEntity> repo) : IRequestHandler<DeleteLinkCommand>
{
    public Task Handle(DeleteLinkCommand request, CancellationToken ct) => repo.DeleteAsync(request.Id, ct);
}