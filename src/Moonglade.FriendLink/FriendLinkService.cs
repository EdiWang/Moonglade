using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Threading.Tasks;

namespace Moonglade.FriendLink
{
    public interface IFriendLinkService
    {
        Task UpdateAsync(Guid id, string newTitle, string newLinkUrl);
    }

    public class FriendLinkService : IFriendLinkService
    {
        private readonly IRepository<FriendLinkEntity> _friendlinkRepo;
        private readonly IBlogAudit _audit;

        public FriendLinkService(
            IRepository<FriendLinkEntity> friendlinkRepo,
            IBlogAudit audit)
        {
            _friendlinkRepo = friendlinkRepo;
            _audit = audit;
        }

        public async Task UpdateAsync(Guid id, string newTitle, string newLinkUrl)
        {
            if (!Uri.IsWellFormedUriString(newLinkUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException($"{nameof(newLinkUrl)} is not a valid url.");
            }

            var link = await _friendlinkRepo.GetAsync(id);
            if (link is not null)
            {
                link.Title = newTitle;
                link.LinkUrl = Helper.SterilizeLink(newLinkUrl);

                await _friendlinkRepo.UpdateAsync(link);
                await _audit.AddEntry(BlogEventType.Content, BlogEventId.FriendLinkUpdated, "FriendLink updated.");
            }
        }
    }
}
