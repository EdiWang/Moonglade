using System;
using System.Collections.Generic;
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
    public class PingbackService : MoongladeService
    {
        private readonly IMoongladeNotificationClient _notificationClient;
        private readonly IPingbackReceiver _pingbackReceiver;
        private readonly IRepository<PingbackHistoryEntity> _pingbackRepository;
        private readonly IRepository<PostEntity> _postRepository;

        public PingbackService(
            ILogger<PingbackService> logger,
            IPingbackReceiver pingbackReceiver,
            IRepository<PingbackHistoryEntity> pingbackRepository,
            IRepository<PostEntity> postRepository,
            IMoongladeNotificationClient notificationClient = null) : base(logger)
        {
            _notificationClient = notificationClient;
            _pingbackReceiver = pingbackReceiver;
            _pingbackRepository = pingbackRepository;
            _postRepository = postRepository;
        }

        public async Task<PingbackServiceResponse> ProcessReceivedPingbackAsync(HttpContext context)
        {
            var response = await _pingbackReceiver.ValidatePingRequest(context);
            if (response == PingbackValidationResult.ValidPingRequest)
            {
                Logger.LogInformation($"Pingback attempt from '{context.Connection.RemoteIpAddress}' is valid");

                var pingRequest = await _pingbackReceiver.GetPingRequest();
                var postResponse = TryGetPostIdTitle(pingRequest.TargetUrl, out var idTitleTuple);
                if (postResponse)
                {
                    Logger.LogInformation($"Post '{idTitleTuple.Id}:{idTitleTuple.Title}' is found for ping.");

                    _pingbackReceiver.OnPingSuccess += async (sender, args) => await SavePingbackRecordAsync(
                        new PingbackRequest
                        {
                            Domain = args.Domain,
                            SourceUrl = args.PingRequest.SourceUrl,
                            SourceTitle = args.PingRequest.SourceDocumentInfo.Title,
                            TargetPostId = idTitleTuple.Id,
                            TargetPostTitle = idTitleTuple.Title,
                            SourceIp = context.Connection.RemoteIpAddress.ToString()
                        });

                    return _pingbackReceiver.ProcessReceivedPingback(
                        pingRequest,
                        () => true,
                        () => HasAlreadyBeenPinged(idTitleTuple.Id, pingRequest.SourceUrl, pingRequest.TargetUrl));
                }

                Logger.LogError($"Can not get post id and title for url '{pingRequest.TargetUrl}'");
                return PingbackServiceResponse.GenericError;
            }

            return PingbackServiceResponse.InvalidPingRequest;
        }

        public Task<Response<IReadOnlyList<PingbackHistory>>> GetReceivedPingbacksAsync()
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
                    return new FailedResponse((int)ResponseFailureCode.PingbackRecordNotFound);
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
                                                p.PostPublish.IsPublished &&
                                                p.PostPublish.PubDateUtc.Value.Year == response.Item.PubDate.Year &&
                                                p.PostPublish.PubDateUtc.Value.Month == response.Item.PubDate.Month &&
                                                p.PostPublish.PubDateUtc.Value.Day == response.Item.PubDate.Day &&
                                                !p.PostPublish.IsDeleted);

            if (null == post)
            {
                idTitleTuple = default((Guid, string));
                return false;
            }

            idTitleTuple = (post.Id, post.Title);
            return true;
        }

        private async Task NotifyAdminAsync(Guid pingbackId)
        {
            var pingback = await _pingbackRepository.SelectFirstOrDefaultAsync(new PingbackHistorySpec(pingbackId),
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

        private async Task SavePingbackRecordAsync(PingbackRequest request)
        {
            var pid = Guid.NewGuid();
            var rpb = new PingbackHistoryEntity
            {
                Domain = request.Domain,
                SourceIp = request.SourceIp,
                Id = pid,
                PingTimeUtc = DateTime.UtcNow,
                SourceTitle = request.SourceTitle,
                SourceUrl = request.SourceUrl,
                TargetPostId = request.TargetPostId,
                TargetPostTitle = request.TargetPostTitle
            };

            _pingbackRepository.Add(rpb);

            await NotifyAdminAsync(pid);
        }

        private bool HasAlreadyBeenPinged(Guid postId, string sourceUrl, string sourceIp)
        {
            var any = _pingbackRepository.Any(p => p.TargetPostId == postId &&
                                                   p.SourceUrl == sourceUrl &&
                                                   p.SourceIp == sourceIp);
            return any;
        }

        private class PingbackRequest
        {
            public string Domain { get; set; }
            public string SourceUrl { get; set; }
            public string SourceTitle { get; set; }
            public Guid TargetPostId { get; set; }
            public string TargetPostTitle { get; set; }
            public string SourceIp { get; set; }
        }
    }
}