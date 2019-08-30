using System;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Newtonsoft.Json;

namespace Moonglade.Core
{
    public class EmailNotificationClient : IMoongladeNotificationClient
    {
        private readonly HttpClient _httpClient;

        public bool IsEnabled { get; set; }

        private readonly ILogger<EmailNotificationClient> _logger;

        private readonly IBlogConfig _blogConfig;

        public EmailNotificationClient(
            ILogger<EmailNotificationClient> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            HttpClient httpClient)
        {
            _logger = logger;
            _blogConfig = blogConfig;
            if (settings.Value.Notification.Enabled)
            {
                if (Uri.IsWellFormedUriString(settings.Value.Notification.ApiEndpoint, UriKind.Absolute))
                {
                    if (!settings.Value.Notification.ApiEndpoint.EndsWith("/"))
                    {
                        throw new FormatException("ApiEndpoint URL must end with a slash '/'.");
                    }

                    httpClient.BaseAddress = new Uri(settings.Value.Notification.ApiEndpoint);
                }
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"Moonglade/{Utils.AppVersion}");
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.Value.Notification.ApiKey);
                _httpClient = httpClient;

                if (_blogConfig.EmailSettings.EnableEmailSending)
                {
                    IsEnabled = true;
                }
            }
        }

        internal class NotificationContent : StringContent
        {
            public NotificationContent(NotificationRequest req) :
                base(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
            { }
        }

        internal class NotificationRequest
        {
            public string AdminEmail { get; set; }
            public string EmailDisplayName { get; set; }
        }

        internal class PingNotificationRequest : NotificationRequest
        {
            public PingNotificationRequest(
                string targetPostTitle, DateTime pingTimeUtc, string domain, string sourceIp, string sourceUrl, string sourceTitle)
            {
                TargetPostTitle = targetPostTitle;
                PingTimeUtc = pingTimeUtc;
                Domain = domain;
                SourceIp = sourceIp;
                SourceUrl = sourceUrl;
                SourceTitle = sourceTitle;
            }

            public string TargetPostTitle { get; set; }

            public DateTime PingTimeUtc { get; set; }

            public string Domain { get; set; }

            public string SourceIp { get; set; }

            public string SourceUrl { get; set; }

            public string SourceTitle { get; set; }
        }

        internal class CommentReplyNotificationRequest : NotificationRequest
        {
            public CommentReplyNotificationRequest(
                string email, string commentContent, string title, string replyContent, string postLink)
            {
                Email = email;
                CommentContent = commentContent;
                Title = title;
                ReplyContent = replyContent;
                PostLink = postLink;
            }

            public string Email { get; set; }

            public string CommentContent { get; set; }

            public string Title { get; set; }

            public string ReplyContent { get; set; }

            public string PostLink { get; set; }
        }

        internal class NewCommentNotificationRequest : NotificationRequest
        {
            public NewCommentNotificationRequest(
                string username, string email, string ipAddress, string postTitle, string commentContent, DateTime createOnUtc)
            {
                Username = username;
                Email = email;
                IpAddress = ipAddress;
                PostTitle = postTitle;
                CommentContent = commentContent;
                CreateOnUtc = createOnUtc;
            }

            public string Username { get; set; }

            public string Email { get; set; }

            public string IpAddress { get; set; }

            public string PostTitle { get; set; }

            public string CommentContent { get; set; }

            public DateTime CreateOnUtc { get; set; }
        }

        private HttpRequestMessage BuildNotificationRequest(string method, Func<NotificationRequest> request)
        {
            var nf = request();
            nf.EmailDisplayName = _blogConfig.EmailSettings.EmailDisplayName;
            nf.AdminEmail = _blogConfig.EmailSettings.AdminEmail;

            var req = new HttpRequestMessage(HttpMethod.Post, method)
            {
                Content = new NotificationContent(nf)
            };
            return req;
        }

        public async Task<Response> SendTestNotificationAsync()
        {
            if (!IsEnabled)
            {
                return new FailedResponse((int)ResponseFailureCode.EmailSendingDisabled, "Email Sending is disabled.");
            }

            try
            {
                var req = BuildNotificationRequest("test", () => new NotificationRequest());
                var response = await _httpClient.SendAsync(req);

                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsAsync<Response>();
                    return data;
                }

                return new FailedResponse((int)ResponseFailureCode.ApiError, response.StatusCode.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task SendNewCommentNotificationAsync(CommentListItem comment, Func<string, string> funcCommentContentFormat)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning("Skipped SendNewCommentNotificationAsync because Email sending is disabled.");
                await Task.CompletedTask;
            }

            throw new NotImplementedException();
        }

        public async Task SendCommentReplyNotificationAsync(CommentReplyDetail model, string postLink)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning("Skipped SendCommentReplyNotificationAsync because Email sending is disabled.");
                await Task.CompletedTask;
            }

            throw new NotImplementedException();
        }

        public async Task SendPingNotificationAsync(PingbackHistory receivedPingback)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning("Skipped SendPingNotificationAsync because Email sending is disabled.");
                await Task.CompletedTask;
            }

            try
            {
                var req = BuildNotificationRequest("ping", () => new PingNotificationRequest(
                    receivedPingback.TargetPostId.ToString(),
                    receivedPingback.PingTimeUtc,
                    receivedPingback.Domain,
                    receivedPingback.SourceIp,
                    receivedPingback.SourceUrl,
                    receivedPingback.SourceTitle));
                var response = await _httpClient.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error SendPingNotificationAsync, response: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }
}