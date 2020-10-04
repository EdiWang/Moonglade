using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class FriendLinkService : BlogService
    {
        private readonly IRepository<FriendLinkEntity> _friendlinkRepository;
        private readonly IBlogAudit _blogAudit;

        public FriendLinkService(
            ILogger<FriendLinkService> logger,
            IOptions<AppSettings> settings,
            IRepository<FriendLinkEntity> friendlinkRepository,
            IBlogAudit blogAudit) : base(logger, settings)
        {
            _friendlinkRepository = friendlinkRepository;
            _blogAudit = blogAudit;
        }

        public Task<FriendLink> GetAsync(Guid id)
        {
            var item = _friendlinkRepository.SelectFirstOrDefaultAsync(
                new FriendLinkSpec(id), f => new FriendLink
                {
                    Id = f.Id,
                    LinkUrl = f.LinkUrl,
                    Title = f.Title
                });
            return item;
        }

        public Task<IReadOnlyList<FriendLink>> GetAllAsync()
        {
            var item = _friendlinkRepository.SelectAsync(f => new FriendLink
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

            await _friendlinkRepository.AddAsync(fdLink);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
        }

        public async Task DeleteAsync(Guid id)
        {
            await _friendlinkRepository.DeleteAsync(id);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
        }

        public async Task UpdateAsync(Guid id, string newTitle, string newLinkUrl)
        {
            if (!Uri.IsWellFormedUriString(newLinkUrl, UriKind.Absolute))
            {
                throw new InvalidOperationException($"{nameof(newLinkUrl)} is not a valid url.");
            }

            var fdlink = await _friendlinkRepository.GetAsync(id);
            if (null != fdlink)
            {
                fdlink.Title = newTitle;
                fdlink.LinkUrl = newLinkUrl;

                await _friendlinkRepository.UpdateAsync(fdlink);
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");
            }
        }
    }
}
