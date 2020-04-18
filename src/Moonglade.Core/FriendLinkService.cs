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
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Core
{
    public class FriendLinkService : MoongladeService
    {
        private readonly IRepository<FriendLinkEntity> _friendlinkRepository;
        private readonly IMoongladeAudit _moongladeAudit;

        public FriendLinkService(
            ILogger<FriendLinkService> logger,
            IOptions<AppSettings> settings,
            IRepository<FriendLinkEntity> friendlinkRepository, 
            IMoongladeAudit moongladeAudit) : base(logger, settings)
        {
            _friendlinkRepository = friendlinkRepository;
            _moongladeAudit = moongladeAudit;
        }

        public Task<Response<FriendLink>> GetFriendLinkAsync(Guid id)
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

        public Task<Response<IReadOnlyList<FriendLink>>> GetAllFriendLinksAsync()
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
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(title)} can not be empty.");
                }

                if (string.IsNullOrWhiteSpace(linkUrl))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(linkUrl)} can not be empty.");
                }

                if (!Uri.IsWellFormedUriString(linkUrl, UriKind.Absolute))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(linkUrl)} is not a valid url.");
                }

                var fdLink = new FriendLinkEntity
                {
                    Id = Guid.NewGuid(),
                    LinkUrl = linkUrl,
                    Title = title
                };

                await _friendlinkRepository.AddAsync(fdLink);
                await _moongladeAudit.AddAuditEntry(EventType.Settings, EventId.SettingsSavedFriendLink, "FriendLink Settings updated.");

                return new SuccessResponse();
            });
        }

        public Task<Response> DeleteAsync(Guid id)
        {
            return TryExecuteAsync(async () =>
            {
                await _friendlinkRepository.DeleteAsync(id);
                await _moongladeAudit.AddAuditEntry(EventType.Settings, EventId.SettingsSavedFriendLink, "FriendLink Settings updated.");

                return new SuccessResponse();
            }, keyParameter: id);
        }

        public Task<Response> UpdateAsync(Guid id, string newTitle, string newLinkUrl)
        {
            return TryExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(newTitle))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(newTitle)} can not be empty.");
                }

                if (string.IsNullOrWhiteSpace(newLinkUrl))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(newLinkUrl)} can not be empty.");
                }

                if (!Uri.IsWellFormedUriString(newLinkUrl, UriKind.Absolute))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(newLinkUrl)} is not a valid url.");
                }

                var fdlink = await _friendlinkRepository.GetAsync(id);
                if (null != fdlink)
                {
                    fdlink.Title = newTitle;
                    fdlink.LinkUrl = newLinkUrl;

                    await _friendlinkRepository.UpdateAsync(fdlink);
                    await _moongladeAudit.AddAuditEntry(EventType.Settings, EventId.SettingsSavedFriendLink, "FriendLink Settings updated.");

                    return new SuccessResponse();
                }
                return new FailedResponse((int)ResponseFailureCode.FriendLinkNotFound);
            }, keyParameter: id);
        }
    }
}
