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

        public async Task<Response<FriendLinkEntity>> GetFriendLinkAsync(Guid id)
        {
            return await TryExecuteAsync<FriendLinkEntity>(async () =>
            {
                var item = await _friendlinkRepository.GetAsync(id);
                return new SuccessResponse<FriendLinkEntity>(item);
            });
        }

        public async Task<Response<IReadOnlyList<FriendLinkEntity>>> GetAllFriendLinksAsync()
        {
            return await TryExecuteAsync<IReadOnlyList<FriendLinkEntity>>(async () =>
            {
                var item = await _friendlinkRepository.GetAsync();
                return new SuccessResponse<IReadOnlyList<FriendLinkEntity>>(item);
            });
        }

        public async Task<Response> AddFriendLinkAsync(string title, string linkUrl)
        {
            try
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
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(AddFriendLinkAsync)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response> DeleteFriendLinkAsync(Guid id)
        {
            try
            {
                await _friendlinkRepository.DeleteAsync(id);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(DeleteFriendLinkAsync)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response> UpdateFriendLinkAsync(Guid id, string newTitle, string newLinkUrl)
        {
            try
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
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(UpdateFriendLinkAsync)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }
    }
}
