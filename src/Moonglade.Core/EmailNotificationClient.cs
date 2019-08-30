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
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task SendNewCommentNotificationAsync(CommentListItem comment, Func<string, string> funcCommentContentFormat)
        {
            if (IsEnabled)
            {
                throw new NotImplementedException();
            }
        }

        public async Task SendCommentReplyNotificationAsync(CommentReplyDetail model, string postLink)
        {
            if (IsEnabled)
            {
                throw new NotImplementedException();
            }
        }

        public async Task SendPingNotificationAsync(PingbackHistory receivedPingback)
        {
            if (IsEnabled)
            {
                throw new NotImplementedException();
            }
        }
    }
}