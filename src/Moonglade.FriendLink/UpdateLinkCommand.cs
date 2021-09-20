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
    public class UpdateLinkCommand : IRequest
    {
        public UpdateLinkCommand(Guid id, EditLinkRequest model)
        {
            Id = id;
            Model = model;
        }

        public Guid Id { get; set; }

        public EditLinkRequest Model { get; set; }
    }

    public class UpdateLinkCommandHandler : IRequestHandler<UpdateLinkCommand>
    {
        private readonly IRepository<FriendLinkEntity> _friendlinkRepo;
        private readonly IBlogAudit _audit;

        public UpdateLinkCommandHandler(IRepository<FriendLinkEntity> friendlinkRepo, IBlogAudit audit)
        {
            _friendlinkRepo = friendlinkRepo;
            _audit = audit;
        }

        public async Task<Unit> Handle(UpdateLinkCommand request, CancellationToken cancellationToken)
        {
            if (!Uri.IsWellFormedUriString(request.Model.LinkUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException($"{nameof(request.Model.LinkUrl)} is not a valid url.");
            }

            var link = await _friendlinkRepo.GetAsync(request.Id);
            if (link is not null)
            {
                link.Title = request.Model.Title;
                link.LinkUrl = Helper.SterilizeLink(request.Model.LinkUrl);

                await _friendlinkRepo.UpdateAsync(link);
                await _audit.AddEntry(BlogEventType.Content, BlogEventId.FriendLinkUpdated, "FriendLink updated.");
            }

            return Unit.Value;
        }
    }
}
