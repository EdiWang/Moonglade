using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
    public class NotificationClient : IMoongladeNotificationClient
    {
        private readonly HttpClient _httpClient;

        public bool IsEnabled { get; set; }

        private readonly ILogger<NotificationClient> _logger;

        private readonly IBlogConfig _blogConfig;

        public NotificationClient(
            ILogger<NotificationClient> logger,
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
                var req = BuildNotificationRequest(() =>
                    new NotificationRequest<EmptyPayload>(MailMesageTypes.TestMail, EmptyPayload.Default));
                var response = await _httpClient.SendAsync(req);

                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    var dataStr = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<Response>(dataStr, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
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

        public async Task SendNewCommentNotificationAsync(CommentListItem model, Func<string, string> funcCommentContentFormat)
        {
            if (!IsEnabled)
            {
                _logger.LogWarning($"Skipped {nameof(SendNewCommentNotificationAsync)} because Email sending is disabled.");
                await Task.CompletedTask;
            }

            try
            {
                var req = new NewCommentNotificationPayload(
                    model.Username,
                    model.Email,
                    model.IpAddress,
                    model.PostTitle,
                    funcCommentContentFormat(model.CommentContent),
                    model.CreateOnUtc
                );

                await SendNotificationRequest(
                    new NotificationRequest<NewCommentNotificationPayload>(MailMesageTypes.NewCommentNotification, req));
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
                var req = new CommentReplyNotificationPayload(
                    model.Email,
                    model.CommentContent,
                    model.Title,
                    model.ReplyContent,
                    postLink);

                await SendNotificationRequest(
                    new NotificationRequest<CommentReplyNotificationPayload>(MailMesageTypes.AdminReplyNotification, req));
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
                var req = new PingNotificationPayload(
                    model.TargetPostTitle,
                    model.PingTimeUtc,
                    model.Domain,
                    model.SourceIp,
                    model.SourceUrl,
                    model.SourceTitle);

                await SendNotificationRequest(new NotificationRequest<PingNotificationPayload>(MailMesageTypes.BeingPinged, req));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task SendNotificationRequest<T>(NotificationRequest<T> request, [CallerMemberName] string callerMemberName = "") where T : class
        {
            var req = BuildNotificationRequest(() => request);
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error executing request '{callerMemberName}', response: {response.StatusCode}");
            }
        }

        private HttpRequestMessage BuildNotificationRequest<T>(Func<NotificationRequest<T>> request) where T : class
        {
            var nf = request();
            nf.EmailDisplayName = _blogConfig.EmailSettings.EmailDisplayName;
            nf.AdminEmail = _blogConfig.EmailSettings.AdminEmail;

            var req = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new NotificationContent<T>(nf)
            };
            return req;
        }
    }
}