using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

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

            var link = new FriendLinkEntity
            {
                Id = Guid.NewGuid(),
                LinkUrl = Helper.SterilizeLink(linkUrl),
                Title = title
            };

            await _friendlinkRepo.AddAsync(link);
            await _audit.AddAuditEntry(BlogEventType.Settings, BlogEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
        }

        public async Task DeleteAsync(Guid id)
        {
            await _friendlinkRepo.DeleteAsync(id);
            await _audit.AddAuditEntry(BlogEventType.Settings, BlogEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
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
                await _audit.AddAuditEntry(BlogEventType.Settings, BlogEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
            }
        }
    }
}
