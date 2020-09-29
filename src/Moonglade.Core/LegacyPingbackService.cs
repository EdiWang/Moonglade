using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Core.Notification;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Pingback;

namespace Moonglade.Core
{
    public class LegacyPingbackService : BlogService
    {
        private readonly IBlogNotificationClient _notificationClient;
        private readonly IPingbackReceiver _pingbackReceiver;
        private readonly IRepository<PingbackHistoryEntity> _pingbackRepository;
        private readonly IRepository<PostEntity> _postRepository;

        public LegacyPingbackService(
            ILogger<LegacyPingbackService> logger,
            IPingbackReceiver pingbackReceiver,
            IRepository<PingbackHistoryEntity> pingbackRepository,
            IRepository<PostEntity> postRepository,
            IBlogNotificationClient notificationClient = null) : base(logger)
        {
            _notificationClient = notificationClient;
            _pingbackReceiver = pingbackReceiver;
            _pingbackRepository = pingbackRepository;
            _postRepository = postRepository;
        }

        public async Task<PingbackResponse> ProcessReceivedPayloadAsync(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var requestBody = await new StreamReader(context.Request.Body, Encoding.Default).ReadToEndAsync();

            var response = _pingbackReceiver.ValidatePingRequest(requestBody, ip);
            if (response == PingbackValidationResult.Valid)
            {
                Logger.LogInformation($"Pingback attempt from '{ip}' is valid");

                var pingRequest = await _pingbackReceiver.GetPingRequest();
                var postResponse = TryGetPostIdTitle(pingRequest.TargetUrl, out var idTitleTuple);
                if (postResponse)
                {
                    Logger.LogInformation($"Post '{idTitleTuple.Id}:{idTitleTuple.Title}' is found for ping.");

                    _pingbackReceiver.OnPingSuccess += async (sender, args) => await SavePingbackRecordAsync(
                        new PingbackHistoryEntity
                        {
                            Id = Guid.NewGuid(),
                            PingTimeUtc = DateTime.UtcNow,
                            Domain = args.Domain,
                            SourceUrl = args.PingRequest.SourceUrl,
                            SourceTitle = args.PingRequest.SourceDocumentInfo.Title,
                            TargetPostId = idTitleTuple.Id,
                            TargetPostTitle = idTitleTuple.Title,
                            SourceIp = ip
                        });

                    return _pingbackReceiver.ReceivingPingback(
                        pingRequest,
                        () => true,
                        () => HasAlreadyBeenPinged(idTitleTuple.Id, pingRequest.SourceUrl, pingRequest.TargetUrl));
                }

                Logger.LogError($"Can not get post id and title for url '{pingRequest.TargetUrl}'");
                return PingbackResponse.GenericError;
            }

            return PingbackResponse.InvalidPingRequest;
        }

        public Task<Response<IReadOnlyList<PingbackHistory>>> GetPingbackHistoryAsync()
        {
            return TryExecuteAsync<IReadOnlyList<PingbackHistory>>(async () =>
            {
                var list = await _pingbackRepository.SelectAsync(p => new PingbackHistory
                {
                    Id = p.Id,
                    Domain = p.Domain,
                    PingTimeUtc = p.PingTimeUtc,
                    SourceIp = p.SourceIp,
                    SourceTitle = p.SourceTitle,
                    SourceUrl = p.SourceUrl,
                    TargetPostTitle = p.TargetPostTitle
                });
                return new SuccessResponse<IReadOnlyList<PingbackHistory>>(list);
            });
        }

        public Response DeleteReceivedPingback(Guid pingbackId)
        {
            return TryExecute(() =>
            {
                Logger.LogInformation($"Deleting pingback {pingbackId}.");
                var rows = _pingbackRepository.Delete(pingbackId);
                if (rows == -1)
                {
                    Logger.LogWarning($"Pingback id {pingbackId} not found, skip delete operation.");
                    return new FailedResponse((int)FaultCode.PingbackRecordNotFound);
                }

                return new Response { IsSuccess = rows > 0 };
            }, keyParameter: pingbackId);
        }

        private bool TryGetPostIdTitle(string url, out (Guid Id, string Title) idTitleTuple)
        {
            var response = Utils.GetSlugInfoFromPostUrl(url);
            if (!response.IsSuccess)
            {
                idTitleTuple = default((Guid, string));
                return false;
            }

            var post = _postRepository.Get(p => p.Slug == response.Item.Slug &&
                                                p.IsPublished &&
                                                p.PubDateUtc.Value.Year == response.Item.PubDate.Year &&
                                                p.PubDateUtc.Value.Month == response.Item.PubDate.Month &&
                                                p.PubDateUtc.Value.Day == response.Item.PubDate.Day &&
                                                !p.IsDeleted);

            if (null == post)
            {
                idTitleTuple = default((Guid, string));
                return false;
            }

            idTitleTuple = (post.Id, post.Title);
            return true;
        }

        private async Task SavePingbackRecordAsync(PingbackHistoryEntity request)
        {
            await _pingbackRepository.AddAsync(request);

            var pingback = await _pingbackRepository.SelectFirstOrDefaultAsync(new PingbackHistorySpec(request.Id),
                p => new PingbackHistory
                {
                    Id = p.Id,
                    Domain = p.Domain,
                    PingTimeUtc = p.PingTimeUtc,
                    SourceIp = p.SourceIp,
                    SourceTitle = p.SourceTitle,
                    SourceUrl = p.SourceUrl,
                    TargetPostTitle = p.TargetPostTitle
                });

            if (null != _notificationClient)
            {
                await _notificationClient.SendPingNotificationAsync(pingback);
            }
        }

        private bool HasAlreadyBeenPinged(Guid postId, string sourceUrl, string sourceIp)
        {
            var any = _pingbackRepository.Any(p => p.TargetPostId == postId &&
                                                   p.SourceUrl == sourceUrl &&
                                                   p.SourceIp == sourceIp);
            return any;
        }
    }
}