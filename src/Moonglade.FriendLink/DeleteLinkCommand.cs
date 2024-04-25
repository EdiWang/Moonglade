using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.FriendLink;

public record DeleteLinkCommand(Guid Id) : IRequest;

public class DeleteLinkCommandHandler(MoongladeRepository<FriendLinkEntity> repo) : IRequestHandler<DeleteLinkCommand>
{
    public Task Handle(DeleteLinkCommand request, CancellationToken ct) => repo.DeleteAsync(request.Id, ct);
}