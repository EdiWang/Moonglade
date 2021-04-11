using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Notification.Client
{
    public class NotificationClient : IBlogNotificationClient
    {
        private readonly HttpClient _httpClient;
        private readonly bool _isEnabled;
        private readonly ILogger<NotificationClient> _logger;
        private readonly IBlogConfig _blogConfig;

        public NotificationClient(
            ILogger<NotificationClient> logger,
            IBlogConfig blogConfig,
            HttpClient httpClient)
        {
            _logger = logger;
            _blogConfig = blogConfig;
            if (_blogConfig.NotificationSettings.EnableEmailSending)
            {
                if (Uri.IsWellFormedUriString(_blogConfig.NotificationSettings.AzureFunctionEndpoint, UriKind.Absolute))
                {
                    httpClient.BaseAddress = new(_blogConfig.NotificationSettings.AzureFunctionEndpoint);
                }

                httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"Moonglade/{Helper.AppVersion}");
                _httpClient = httpClient;

                _isEnabled = true;
            }
        }

        public async Task TestNotificationAsync()
        {
            try
            {
                var req = BuildRequest(() =>
                    new NotificationRequest<EmptyPayload>(MailMesageTypes.TestMail, EmptyPayload.Default));
                var response = await _httpClient.SendAsync(req);

                if (response.IsSuccessStatusCode)
                {
                    var dataStr = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Test email is sent, server response: '{dataStr}'");
                }
                else
                {
                    throw new($"Test email sending failed, response code: '{response.StatusCode}'");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task NotifyCommentAsync(
            string username, string email, string ipAddress, string postTitle, string commentContent, DateTime createTimeUtc)
        {
            var payload = new CommentPayload(
                username,
                email,
                ipAddress,
                postTitle,
                ContentProcessor.MarkdownToContent(commentContent, ContentProcessor.MarkdownConvertType.Html),
                createTimeUtc
            );

            try
            {
                await SendAsync(new NotificationRequest<CommentPayload>(MailMesageTypes.NewCommentNotification, payload));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task NotifyCommentReplyAsync(string email, string commentContent, string title, string replyContentHtml, string postLink)
        {
            var payload = new CommentReplyPayload(
                email,
                commentContent,
                title,
                replyContentHtml,
                postLink);

            try
            {
                await SendAsync(new NotificationRequest<CommentReplyPayload>(MailMesageTypes.AdminReplyNotification, payload));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task NotifyPingbackAsync(string targetPostTitle, DateTime pingTimeUtc, string domain, string sourceIp, string sourceUrl, string sourceTitle)
        {
            var payload = new PingPayload(
                targetPostTitle,
                pingTimeUtc,
                domain,
                sourceIp,
                sourceUrl,
                sourceTitle);

            try
            {
                await SendAsync(new NotificationRequest<PingPayload>(MailMesageTypes.BeingPinged, payload));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task SendAsync<T>(NotificationRequest<T> request, [CallerMemberName] string callerMemberName = "") where T : class
        {
            if (!_isEnabled)
            {
                _logger.LogWarning($"Skipped '{callerMemberName}' because Email sending is disabled.");
                return;
            }

            var req = BuildRequest(() => request);
            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error executing request '{callerMemberName}', response: {response.StatusCode}");
            }
        }

        private HttpRequestMessage BuildRequest<T>(Func<NotificationRequest<T>> request) where T : class
        {
            var nf = request();
            nf.EmailDisplayName = _blogConfig.NotificationSettings.EmailDisplayName;
            nf.AdminEmail = _blogConfig.GeneralSettings.OwnerEmail;

            var req = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new NotificationContent<T>(nf)
            };
            return req;
        }
    }
}