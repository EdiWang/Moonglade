using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Blog.Pingback;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class PingbackService : MoongladeService
    {
        private readonly EmailService _emailService;

        private readonly PostService _postService;

        private readonly PingbackReceiver _pingbackReceiver;

        public PingbackService(MoongladeDbContext context,
            ILogger<PingbackService> logger,
            EmailService emailService,
            PostService postService,
            PingbackReceiver pingbackReceiver) : base(context, logger)
        {
            _emailService = emailService;
            _postService = postService;
            _pingbackReceiver = pingbackReceiver;
        }

        public async Task<PingbackServiceResponse> ProcessReceivedPingback(HttpContext context)
        {
            var response = await _pingbackReceiver.ValidatePingRequest(context);
            if (response == PingbackServiceResponse.ValidPingRequest)
            {
                Logger.LogInformation($"Pingback Attempt from {context.Connection.RemoteIpAddress} is valid");

                var pingRequest = await _pingbackReceiver.GetPingRequest();
                var postResponse = _postService.GetPost(pingRequest.TargetUrl);
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
                            () => null != post,
                            () => HasAlreadyBeenPinged(post.Id, pingRequest.SourceUrl, pingRequest.TargetUrl));
                }
                return PingbackServiceResponse.GenericError;
            }
            return response;
        }

        public async Task<Response> SavePingbackRecord(string domain, string sourceUrl, string sourceTitle, Guid targetPostId, string targetPostTitle, string sourceIp)
        {
            try
            {
                var pid = Guid.NewGuid();
                var rpb = new PingbackHistory
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

                Context.PingbackHistory.Add(rpb);
                var rows = Context.SaveChanges();

                if (rows > 0)
                {
                    await NotifyAdminForReceivedPing(pid);
                    return new SuccessResponse();
                }
                return new FailedResponse((int)ResponseFailureCode.DataOperationFailed);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error SavePingbackRecord");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public List<PingbackHistory> GetReceivedPingbacks()
        {
            return Context.PingbackHistory.AsNoTracking().ToList();
        }

        private async Task NotifyAdminForReceivedPing(Guid pingbackId)
        {
            var pingback = Context.PingbackHistory.Find(pingbackId);
            await _emailService.SendPingNotification(pingback);
        }

        public Response DeleteReceivedPingback(Guid pingbackId)
        {
            try
            {
                var pingback = Context.PingbackHistory.Find(pingbackId);
                if (null != pingback)
                {
                    Logger.LogInformation($"Deleting pingback {pingbackId}.");
                    Context.PingbackHistory.Remove(pingback);
                    int rows = Context.SaveChanges();
                    return new Response { IsSuccess = rows > 0 };
                }

                Logger.LogWarning($"Pingback id {pingbackId} not found, skip delete operation.");
                return new FailedResponse((int)ResponseFailureCode.PingbackRecordNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error DeleteReceivedPingback({pingbackId})", e);
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        private bool HasAlreadyBeenPinged(Guid postId, string sourceUrl, string sourceIp)
        {
            var any = Context.PingbackHistory.Any(p => p.TargetPostId == postId &&
                                                       p.SourceUrl == sourceUrl &&
                                                       p.SourceIp == sourceIp);
            return any;
        }
    }
}
