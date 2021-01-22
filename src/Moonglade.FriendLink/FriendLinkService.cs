using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.FriendLink
{
    public interface IFriendLinkService
    {
        Task<Link> GetAsync(Guid id);
        Task<IReadOnlyList<Link>> GetAllAsync();
        Task AddAsync(string title, string linkUrl);
        Task DeleteAsync(Guid id);
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

        public Task<Link> GetAsync(Guid id)
        {
            var item = _friendlinkRepo.SelectFirstOrDefaultAsync(
                new FriendLinkSpec(id), f => new Link
                {
                    Id = f.Id,
                    LinkUrl = f.LinkUrl,
                    Title = f.Title
                });
            return item;
        }

        public Task<IReadOnlyList<Link>> GetAllAsync()
        {
            var item = _friendlinkRepo.SelectAsync(f => new Link
            {
                Id = f.Id,
                LinkUrl = f.LinkUrl,
                Title = f.Title
            });
            return item;
        }

        public async Task AddAsync(string title, string linkUrl)
        {
            if (!Uri.IsWellFormedUriString(linkUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException($"{nameof(linkUrl)} is not a valid url.");
            }

            var fdLink = new FriendLinkEntity
            {
                Id = Guid.NewGuid(),
                LinkUrl = linkUrl,
                Title = title
            };

            await _friendlinkRepo.AddAsync(fdLink);
            await _audit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
        }

        public async Task DeleteAsync(Guid id)
        {
            await _friendlinkRepo.DeleteAsync(id);
            await _audit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
        }

        public async Task UpdateAsync(Guid id, string newTitle, string newLinkUrl)
        {
            if (!Uri.IsWellFormedUriString(newLinkUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException($"{nameof(newLinkUrl)} is not a valid url.");
            }

            var fdlink = await _friendlinkRepo.GetAsync(id);
            if (fdlink is not null)
            {
                fdlink.Title = newTitle;
                fdlink.LinkUrl = newLinkUrl;

                await _friendlinkRepo.UpdateAsync(fdlink);
                await _audit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
            }
        }
    }
}
