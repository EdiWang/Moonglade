using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.FriendLink
{
    public class GetAllLinksQuery : IRequest<IReadOnlyList<Link>>
    {

    }

    public class GetAllLinksQueryHandler : IRequestHandler<GetAllLinksQuery, IReadOnlyList<Link>>
    {
        private readonly IRepository<FriendLinkEntity> _friendlinkRepo;

        public GetAllLinksQueryHandler(IRepository<FriendLinkEntity> friendlinkRepo)
        {
            _friendlinkRepo = friendlinkRepo;
        }

        public Task<IReadOnlyList<Link>> Handle(GetAllLinksQuery request, CancellationToken cancellationToken)
        {
            var data = _friendlinkRepo.SelectAsync(f => new Link
            {
                Id = f.Id,
                LinkUrl = f.LinkUrl,
                Title = f.Title
            });

            return data;
        }
    }
}