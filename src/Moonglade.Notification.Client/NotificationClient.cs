using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moonglade.Configuration;
using Moonglade.Utils;
using System;
using System.Net.Http;
using System.Threading.Tasks;

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
                    httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
                    httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"Moonglade/{Helper.AppVersion}");
                    _httpClient = httpClient;

                    _isEnabled = true;
                }
                else
                {
                    _isEnabled = false;
                    _logger.LogError($"'{_blogConfig.NotificationSettings.AzureFunctionEndpoint}' is not a valid URI for notification endpoint, email sending has been disabled.");
                }
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

        public async Task<HttpResponseMessage> SendNotification<T>(MailMesageTypes type, T payload) where T : class
        {
            try
            {
                var nr = new NotificationRequest<T>(type, payload);
                var response = await SendAsync(nr);
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<HttpResponseMessage> SendAsync<T>(NotificationRequest<T> request) where T : class
        {
            if (!_isEnabled) { return null; }

            request.EmailDisplayName = _blogConfig.NotificationSettings.EmailDisplayName;
            request.AdminEmail = _blogConfig.GeneralSettings.OwnerEmail;

            var req = new HttpRequestMessage(HttpMethod.Post, string.Empty)
            {
                Content = new NotificationContent<T>(request)
            };

            var response = await _httpClient.SendAsync(req);

            return response;
        }
    }
}