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
            public NotificationRequest(string adminEmail, string emailDisplayName)
            {
                AdminEmail = adminEmail;
                EmailDisplayName = emailDisplayName;
            }

            public string AdminEmail { get; set; }
            public string EmailDisplayName { get; set; }
        }

        public async Task<Response> SendTestNotificationAsync()
        {
            try
            {
                var method = "test";
                var req = new HttpRequestMessage(HttpMethod.Post, method)
                {
                    Content = new NotificationContent(
                        new NotificationRequest(
                            _blogConfig.EmailSettings.AdminEmail,
                            _blogConfig.EmailSettings.EmailDisplayName))
                };
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
            throw new NotImplementedException();
        }

        public async Task SendCommentReplyNotificationAsync(CommentReplyDetail model, string postLink)
        {
            throw new NotImplementedException();
        }

        public async Task SendPingNotificationAsync(PingbackHistory receivedPingback)
        {
            throw new NotImplementedException();
        }
    }
}