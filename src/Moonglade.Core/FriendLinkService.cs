using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class FriendLinkService : MoongladeService
    {
        private readonly IRepository<FriendLinkEntity> _friendlinkRepository;

        public FriendLinkService(
            ILogger<FriendLinkService> logger,
            IOptions<AppSettings> settings,
            IRepository<FriendLinkEntity> friendlinkRepository) : base(logger, settings)
        {
            _friendlinkRepository = friendlinkRepository;
        }

        public Task<Response<FriendLinkEntity>> GetFriendLinkAsync(Guid id)
        {
            return TryExecuteAsync<FriendLinkEntity>(async () =>
            {
                var item = await _friendlinkRepository.GetAsync(id);
                return new SuccessResponse<FriendLinkEntity>(item);
            });
        }

        public Task<Response<IReadOnlyList<FriendLinkEntity>>> GetAllFriendLinksAsync()
        {
            return TryExecuteAsync<IReadOnlyList<FriendLinkEntity>>(async () =>
            {
                var item = await _friendlinkRepository.GetAsync();
                return new SuccessResponse<IReadOnlyList<FriendLinkEntity>>(item);
            });
        }

        public Task<Response> AddFriendLinkAsync(string title, string linkUrl)
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
                return new SuccessResponse();
            });
        }

        public Task<Response> DeleteFriendLinkAsync(Guid id)
        {
            return TryExecuteAsync(async () =>
            {
                await _friendlinkRepository.DeleteAsync(id);
                return new SuccessResponse();
            }, keyParameter: id);
        }

        public Task<Response> UpdateFriendLinkAsync(Guid id, string newTitle, string newLinkUrl)
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

                    var rows = _friendlinkRepository.UpdateAsync(fdlink);
                    return new SuccessResponse();
                }
                return new FailedResponse((int)ResponseFailureCode.FriendLinkNotFound);
            }, keyParameter: id);
        }
    }
}
