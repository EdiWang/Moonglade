using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core.Notification
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
                        throw new FormatException($"{nameof(settings.Value.Notification.ApiEndpoint)} must end with a slash '/'.");
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
                _logger.LogWarning($"Skipped {nameof(SendNewCommentNotificationAsync)} because Email sending is disabled.");
                await Task.CompletedTask;
            }

            try
            {
                var req = new NewCommentNotificationRequest(
                    comment.Username,
                    comment.Email,
                    comment.IpAddress,
                    comment.PostTitle,
                    funcCommentContentFormat(comment.CommentContent),
                    comment.CreateOnUtc
                );

                await SendNotificationRequest("newcomment", req);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task SendCommentReplyNotificationAsync(CommentReplyDetail model, string postLink)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning($"Skipped {nameof(SendCommentReplyNotificationAsync)} because Email sending is disabled.");
                await Task.CompletedTask;
            }

            try
            {
                var req = new CommentReplyNotificationRequest(
                    model.Email,
                    model.CommentContent,
                    model.Title,
                    model.ReplyContent,
                    postLink);

                await SendNotificationRequest("commentreply", req);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task SendPingNotificationAsync(PingbackHistory model)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning($"Skipped {nameof(SendPingNotificationAsync)} because Email sending is disabled.");
                await Task.CompletedTask;
            }

            try
            {
                var req = new PingNotificationRequest(
                    model.TargetPostId.ToString(),
                    model.PingTimeUtc,
                    model.Domain,
                    model.SourceIp,
                    model.SourceUrl,
                    model.SourceTitle);

                await SendNotificationRequest("ping", req);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task SendNotificationRequest(string method, NotificationRequest request, [CallerMemberName] string callerMemberName = "")
        {
            var req = BuildNotificationRequest(method, () => request);
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error executing request '{callerMemberName}', response: {response.StatusCode}");
            }
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
    }
}