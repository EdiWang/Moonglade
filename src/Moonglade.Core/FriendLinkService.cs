using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
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
    public class FriendLinkService : MoongladeService
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

        public Task<Response<FriendLink>> GetAsync(Guid id)
        {
            return TryExecuteAsync<FriendLink>(async () =>
            {
                var item = await _friendlinkRepository.SelectFirstOrDefaultAsync(
                    new FriendLinkSpec(id), f => new FriendLink
                    {
                        Id = f.Id,
                        LinkUrl = f.LinkUrl,
                        Title = f.Title
                    });
                return new SuccessResponse<FriendLink>(item);
            });
        }

        public Task<Response<IReadOnlyList<FriendLink>>> GetAllAsync()
        {
            return TryExecuteAsync<IReadOnlyList<FriendLink>>(async () =>
            {
                var item = await _friendlinkRepository.SelectAsync(f => new FriendLink
                {
                    Id = f.Id,
                    LinkUrl = f.LinkUrl,
                    Title = f.Title
                });
                return new SuccessResponse<IReadOnlyList<FriendLink>>(item);
            });
        }

        public Task<Response> AddAsync(string title, string linkUrl)
        {
            return TryExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return new FailedResponse((int)FaultCode.InvalidParameter, $"{nameof(title)} can not be empty.");
                }

                if (string.IsNullOrWhiteSpace(linkUrl))
                {
                    return new FailedResponse((int)FaultCode.InvalidParameter, $"{nameof(linkUrl)} can not be empty.");
                }

                if (!Uri.IsWellFormedUriString(linkUrl, UriKind.Absolute))
                {
                    return new FailedResponse((int)FaultCode.InvalidParameter, $"{nameof(linkUrl)} is not a valid url.");
                }

                var fdLink = new FriendLinkEntity
                {
                    Id = Guid.NewGuid(),
                    LinkUrl = linkUrl,
                    Title = title
                };

                await _friendlinkRepository.AddAsync(fdLink);
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");

                return new SuccessResponse();
            });
        }

        public Task<Response> DeleteAsync(Guid id)
        {
            return TryExecuteAsync(async () =>
            {
                await _friendlinkRepository.DeleteAsync(id);
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");

                return new SuccessResponse();
            }, keyParameter: id);
        }

        public Task<Response> UpdateAsync(Guid id, string newTitle, string newLinkUrl)
        {
            return TryExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(newTitle))
                {
                    return new FailedResponse((int)FaultCode.InvalidParameter, $"{nameof(newTitle)} can not be empty.");
                }

                if (string.IsNullOrWhiteSpace(newLinkUrl))
                {
                    return new FailedResponse((int)FaultCode.InvalidParameter, $"{nameof(newLinkUrl)} can not be empty.");
                }

                if (!Uri.IsWellFormedUriString(newLinkUrl, UriKind.Absolute))
                {
                    return new FailedResponse((int)FaultCode.InvalidParameter, $"{nameof(newLinkUrl)} is not a valid url.");
                }

                var fdlink = await _friendlinkRepository.GetAsync(id);
                if (null != fdlink)
                {
                    fdlink.Title = newTitle;
                    fdlink.LinkUrl = newLinkUrl;

                    await _friendlinkRepository.UpdateAsync(fdlink);
                    await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedFriendLink, "FriendLink Settings updated.");

                    return new SuccessResponse();
                }
                return new FailedResponse((int)FaultCode.FriendLinkNotFound);
            }, keyParameter: id);
        }
    }
}
