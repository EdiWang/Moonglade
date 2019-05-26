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

        private readonly PostService _postService;

        private readonly IPingbackReceiver _pingbackReceiver;

        private readonly IRepository<PingbackHistoryEntity> _pingbackRepository;

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
            if (response == PingbackServiceResponse.ValidPingRequest)
            {
                Logger.LogInformation($"Pingback Attempt from {context.Connection.RemoteIpAddress} is valid");

                var pingRequest = await _pingbackReceiver.GetPingRequest();
                var postResponse = _postService.GetPostIdTitle(pingRequest.TargetUrl);
                if (postResponse.IsSuccess)
                {
                    var post = postResponse.Item;

                    _pingbackReceiver.OnPingSuccess += async (sender, args) => await SavePingbackRecord(
                        args.Domain,
                        args.PingRequest.SourceUrl,
                        args.PingRequest.SourceDocumentInfo.Title,
                        post.Id,
                        post.Title,
                        context.Connection.RemoteIpAddress.ToString());

                    return _pingbackReceiver.ProcessReceivedPingback(
                            pingRequest,
                            () => true,
                            () => HasAlreadyBeenPinged(post.Id, pingRequest.SourceUrl, pingRequest.TargetUrl));
                }

                Logger.LogError(postResponse.Exception, postResponse.Message);
                return PingbackServiceResponse.GenericError;
            }
            return response;
        }

        public async Task<Response> SavePingbackRecord(string domain, string sourceUrl, string sourceTitle, Guid targetPostId, string targetPostTitle, string sourceIp)
        {
            try
            {
                var pid = Guid.NewGuid();
                var rpb = new PingbackHistoryEntity
                {
                    Domain = domain,
                    SourceIp = sourceIp,
                    Id = pid,
                    PingTimeUtc = DateTime.UtcNow,
                    SourceTitle = sourceTitle,
                    SourceUrl = sourceUrl,
                    TargetPostId = targetPostId,
                    TargetPostTitle = targetPostTitle,
                    Direction = "INBOUND"
                };

                _pingbackRepository.Add(rpb);

                await NotifyAdminForReceivedPing(pid);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(SavePingbackRecord)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public Task<IReadOnlyList<PingbackHistoryEntity>> GetReceivedPingbacksAsync()
        {
            return _pingbackRepository.GetAsync();
        }

        private async Task NotifyAdminForReceivedPing(Guid pingbackId)
        {
            var pingback = _pingbackRepository.Get(pingbackId);
            var title = _postService.GetPostTitle(pingback.TargetPostId.GetValueOrDefault());
            if (!string.IsNullOrWhiteSpace(title))
            {
                await _notification.SendPingNotification(pingback, title);
            }
        }

        public Response DeleteReceivedPingback(Guid pingbackId)
        {
            return TryExecute(() =>
            {
                Logger.LogInformation($"Deleting pingback {pingbackId}.");
                int rows = _pingbackRepository.Delete(pingbackId);
                if (rows == -1)
                {
                    Logger.LogWarning($"Pingback id {pingbackId} not found, skip delete operation.");
                    return new FailedResponse((int) ResponseFailureCode.PingbackRecordNotFound);
                }

                return new Response {IsSuccess = rows > 0};
            }, keyParameter: pingbackId);
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
