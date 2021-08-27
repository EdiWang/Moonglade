using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.FriendLink
{
    public class AddLinkCommand : IRequest
    {
        public AddLinkCommand(FriendLinkEditModel model)
        {
            Model = model;
        }

        public FriendLinkEditModel Model { get; set; }
    }

    public class AddLinkCommandHandler : IRequestHandler<AddLinkCommand>
    {
        private readonly IRepository<FriendLinkEntity> _friendlinkRepo;
        private readonly IBlogAudit _audit;

        public AddLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo, IBlogAudit audit)
        {
            _friendlinkRepo = friendlinkRepo;
            _audit = audit;
        }

        public async Task<Unit> Handle(AddLinkCommand request, CancellationToken cancellationToken)
        {
            if (!Uri.IsWellFormedUriString(request.Model.LinkUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException($"{nameof(request.Model.LinkUrl)} is not a valid url.");
            }

            var link = new FriendLinkEntity
            {
                Id = Guid.NewGuid(),
                LinkUrl = Helper.SterilizeLink(request.Model.LinkUrl),
                Title = request.Model.Title
            };

            await _friendlinkRepo.AddAsync(link);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.FriendLinkCreated, "FriendLink created.");

            return Unit.Value;
        }
    }
}
