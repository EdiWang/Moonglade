using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.FriendLink
{
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
        private readonly IBlogAudit _audit;

        public DeleteLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo, IBlogAudit audit)
        {
            _friendlinkRepo = friendlinkRepo;
            _audit = audit;
        }

        public async Task<Unit> Handle(DeleteLinkCommand request, CancellationToken cancellationToken)
        {
            await _friendlinkRepo.DeleteAsync(request.Id);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.FriendLinkDeleted, "FriendLink deleted.");

            return Unit.Value;
        }
    }
}
