using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Blog.Pingback;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Notification;

namespace Moonglade.Core
{
    public class PingbackService : MoongladeService
    {
        private readonly IMoongladeNotification _notification;

        private readonly IPingbackReceiver _pingbackReceiver;

        private readonly IRepository<PingbackHistoryEntity> _pingbackRepository;

        private readonly PostService _postService;

        public PingbackService(
            ILogger<PingbackService> logger,
            IMoongladeNotification notification,
            PostService postService,
            IPingbackReceiver pingbackReceiver,
            IRepository<PingbackHistoryEntity> pingbackRepository) : base(logger)
        {
            _notification = notification;
            _postService = postService;
            _pingbackReceiver = pingbackReceiver;
            _pingbackRepository = pingbackRepository;
        }

        public async Task<PingbackServiceResponse> ProcessReceivedPingback(HttpContext context)
        {
            var response = await _pingbackReceiver.ValidatePingRequest(context);
            if (response == PingbackValidationResult.ValidPingRequest)
            {
                Logger.LogInformation($"Pingback Attempt from {context.Connection.RemoteIpAddress} is valid");

                var pingRequest = await _pingbackReceiver.GetPingRequest();
                var postResponse = _postService.GetPostIdTitle(pingRequest.TargetUrl);
                if (postResponse.IsSuccess)
                {
                    var post = postResponse.Item;

                    _pingbackReceiver.OnPingSuccess += async (sender, args) => await SavePingbackRecord(
                        new PingbackRequest
                        {
                            Domain = args.Domain,
                            SourceUrl = args.PingRequest.SourceUrl,
                            SourceTitle = args.PingRequest.SourceDocumentInfo.Title,
                            TargetPostId = post.Id,
                            TargetPostTitle = post.Title,
                            SourceIp = context.Connection.RemoteIpAddress.ToString()
                        });

                    return _pingbackReceiver.ProcessReceivedPingback(
                        pingRequest,
                        () => true,
                        () => HasAlreadyBeenPinged(post.Id, pingRequest.SourceUrl, pingRequest.TargetUrl));
                }

                Logger.LogError(postResponse.Exception, postResponse.Message);
                return PingbackServiceResponse.GenericError;
            }

            return PingbackServiceResponse.InvalidPingRequest;
        }

        public Task<Response<IReadOnlyList<PingbackHistoryItem>>> GetReceivedPingbacksAsync()
        {
            return TryExecuteAsync<IReadOnlyList<PingbackHistoryItem>>(async () =>
            {
                var list = await _pingbackRepository.SelectAsync(p => new PingbackHistoryItem
                {
                    Id = p.Id,
                    Domain = p.Domain,
                    PingTimeUtc = p.PingTimeUtc,
                    SourceIp = p.SourceIp,
                    SourceTitle = p.SourceTitle,
                    SourceUrl = p.SourceUrl,
                    TargetPostTitle = p.TargetPostTitle
                });
                return new SuccessResponse<IReadOnlyList<PingbackHistoryItem>>(list);
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

        private async Task NotifyAdminForReceivedPing(Guid pingbackId)
        {
            var pingback = _pingbackRepository.Get(pingbackId);
            var title = _postService.GetPostTitle(pingback.TargetPostId);
            if (!string.IsNullOrWhiteSpace(title)) await _notification.SendPingNotificationAsync(pingback, title);
        }

        private async Task SavePingbackRecord(PingbackRequest request)
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

            await NotifyAdminForReceivedPing(pid);
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